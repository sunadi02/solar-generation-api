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
}