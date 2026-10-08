using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGenerationApi.Data;
using SolarGenerationApi.Dtos;
using SolarGenerationApi.Errors;
using SolarGenerationApi.Services;

namespace SolarGenerationApi.Controllers;

[ApiController]
[Authorize(Policy = "readings:read")]
[Route("api/v1/districts")]
public class DistrictsController : ControllerBase
{
    private readonly SolarGenerationDbContext _db;
    private readonly JurisdictionService _scope;

    public DistrictsController(SolarGenerationDbContext db, JurisdictionService scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var district = await _db.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (district is null) return Err.NotFound("District");
        if (!await _scope.Districts(User).AnyAsync(d => d.Id == id)) return Err.OutOfJurisdiction();
        return Ok(new DistrictDto(district.Id, district.Name, district.ProvinceId));
    }

    [HttpGet("{id:int}/grid-substations")]
    public async Task<IActionResult> GridSubstations(int id)
    {
        if (!await _db.Districts.AnyAsync(d => d.Id == id)) return Err.NotFound("District");
        if (!await _scope.Districts(User).AnyAsync(d => d.Id == id)) return Err.OutOfJurisdiction();

        var items = await _scope.GridSubstations(User)
            .Where(s => s.DistrictId == id)
            .OrderBy(s => s.Code)
            .Select(s => new GridSubstationDto(s.Id, s.Name, s.Code, s.DistrictId))
            .ToListAsync();
        return Ok(new ListResponse<GridSubstationDto>(items.Count, items));
    }

        [HttpGet("{id:int}/generation-summary")]
    public async Task<IActionResult> GenerationSummary(int id)
    {
        var district = await _db.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (district is null) return Err.NotFound("District");
        if (!await _scope.Districts(User).AnyAsync(d => d.Id == id)) return Err.OutOfJurisdiction();

        var installations = _db.SolarInstallations.Where(s => s.GridSubstation!.DistrictId == id);
        var count = await installations.CountAsync();
        var capacity = await installations.SumAsync(s => s.CapacityKw);

        var latest = await _db.GenerationReadings
            .Where(r => r.SolarInstallation!.GridSubstation!.DistrictId == id)
            .Where(r => r.Timestamp == _db.GenerationReadings
                .Where(x => x.SolarInstallationId == r.SolarInstallationId)
                .Max(x => (DateTime?)x.Timestamp))
            .Select(r => new { r.SolarInstallationId, r.InstantaneousPowerKw, r.CumulativeEnergyKwh, r.Timestamp })
            .ToListAsync();


        var midnightUtc = DateTime.UtcNow.AddHours(5.5).Date.AddHours(-5.5);
        midnightUtc = DateTime.SpecifyKind(midnightUtc, DateTimeKind.Utc);

        var baseline = await _db.GenerationReadings
            .Where(r => r.SolarInstallation!.GridSubstation!.DistrictId == id && r.Timestamp < midnightUtc)
            .Where(r => r.Timestamp == _db.GenerationReadings
                .Where(x => x.SolarInstallationId == r.SolarInstallationId && x.Timestamp < midnightUtc)
                .Max(x => (DateTime?)x.Timestamp))
            .Select(r => new { r.SolarInstallationId, r.CumulativeEnergyKwh })
            .ToListAsync();
        var baseMap = baseline.ToDictionary(b => b.SolarInstallationId, b => b.CumulativeEnergyKwh);

        var currentPower = latest.Sum(l => l.InstantaneousPowerKw);
        var energyToday = latest.Sum(l => l.CumulativeEnergyKwh - baseMap.GetValueOrDefault(l.SolarInstallationId, 0));
        DateTime? asOf = latest.Count > 0 ? latest.Max(l => l.Timestamp) : null;

        return Ok(new DistrictGenerationSummaryDto(district.Id, district.Name, count,
            Math.Round(capacity, 2), Math.Round(currentPower, 3), Math.Round(energyToday, 3), asOf));
    }
}