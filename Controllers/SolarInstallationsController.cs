using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGenerationApi.Data;
using SolarGenerationApi.Dtos;
using SolarGenerationApi.Errors;
using SolarGenerationApi.Models;
using SolarGenerationApi.Services;

namespace SolarGenerationApi.Controllers;

[ApiController]
[Route("api/v1/solar-installations")]
public class SolarInstallationsController : ControllerBase
{
    private readonly SolarGenerationDbContext _db;
    private readonly JurisdictionService _scope;

    public SolarInstallationsController(SolarGenerationDbContext db, JurisdictionService scope)
    {
        _db = db;
        _scope = scope;
    }

    private static readonly Expression<Func<SolarInstallation, InstallationDto>> ToDto = s =>
        new InstallationDto(s.Id, s.MeterIdentifier, s.OwnerName, s.Address, s.CapacityKw,
            s.InstalledOn, s.UpdatedAt, s.GridSubstationId);

    private static readonly Expression<Func<GenerationReading, ReadingDto>> ToReadingDto = r =>
        new ReadingDto(r.Id, r.SolarInstallationId, r.Timestamp, r.InstantaneousPowerKw,
            r.CumulativeEnergyKwh, r.Voltage);

    private static IActionResult? ValidatePaging(int page, int pageSize) =>
        page < 1 || pageSize < 1 || pageSize > 100
            ? Err.Make(400, "VALIDATION_ERROR", "page must be at least 1 and page_size must be between 1 and 100.")
            : null;

    
    private async Task<IActionResult?> CheckAccess(int id)
    {
        if (!await _db.SolarInstallations.AnyAsync(s => s.Id == id))
            return Err.NotFound("Solar installation");
        if (!await _scope.Installations(User).AnyAsync(s => s.Id == id))
            return Err.OutOfJurisdiction();
        return null;
    }

    [HttpGet]
    [Authorize(Policy = "readings:read")]
    public async Task<IActionResult> List(int? province, int? district, int? substation, string? sort,
        int page = 1, [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        var invalid = ValidatePaging(page, pageSize);
        if (invalid != null) return invalid;

        var q = _scope.Installations(User);
        if (province.HasValue) q = q.Where(s => s.GridSubstation!.District!.ProvinceId == province.Value);
        if (district.HasValue) q = q.Where(s => s.GridSubstation!.DistrictId == district.Value);
        if (substation.HasValue) q = q.Where(s => s.GridSubstationId == substation.Value);

        IOrderedQueryable<SolarInstallation>? ordered = sort switch
        {
            null or "id" => q.OrderBy(s => s.Id),
            "-id" => q.OrderByDescending(s => s.Id),
            "meter" => q.OrderBy(s => s.MeterIdentifier),
            "-meter" => q.OrderByDescending(s => s.MeterIdentifier),
            "capacity" => q.OrderBy(s => s.CapacityKw),
            "-capacity" => q.OrderByDescending(s => s.CapacityKw),
            "installed_on" => q.OrderBy(s => s.InstalledOn),
            "-installed_on" => q.OrderByDescending(s => s.InstalledOn),
            _ => null
        };
        if (ordered is null)
            return Err.Make(400, "VALIDATION_ERROR",
                "sort must be one of: id, meter, capacity, installed_on (prefix with - for descending).");

        var total = await q.CountAsync();
        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToListAsync();

        return Ok(new PagedResponse<InstallationDto>(total, page, pageSize, items,
            PagingLinks.Build(Request, page, pageSize, total)));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "readings:read")]
    public async Task<IActionResult> Get(int id)
    {
        var denied = await CheckAccess(id);
        if (denied != null) return denied;

        var dto = await _db.SolarInstallations.AsNoTracking()
            .Where(s => s.Id == id).Select(ToDto).FirstAsync();
        return Ok(dto);
    }

    
}