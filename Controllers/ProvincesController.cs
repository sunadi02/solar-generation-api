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
[Route("api/v1/provinces")]
public class ProvincesController : ControllerBase
{
    private readonly SolarGenerationDbContext _db;
    private readonly JurisdictionService _scope;

    public ProvincesController(SolarGenerationDbContext db, JurisdictionService scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _scope.Provinces(User)
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceDto(p.Id, p.Name))
            .ToListAsync();
        return Ok(new ListResponse<ProvinceDto>(items.Count, items));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var province = await _db.Provinces.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (province is null) return Err.NotFound("Province");
        if (!await _scope.Provinces(User).AnyAsync(p => p.Id == id)) return Err.OutOfJurisdiction();
        return Ok(new ProvinceDto(province.Id, province.Name));
    }

    [HttpGet("{id:int}/districts")]
    public async Task<IActionResult> Districts(int id)
    {
        if (!await _db.Provinces.AnyAsync(p => p.Id == id)) return Err.NotFound("Province");
        if (!await _scope.Provinces(User).AnyAsync(p => p.Id == id)) return Err.OutOfJurisdiction();

        var items = await _scope.Districts(User)
            .Where(d => d.ProvinceId == id)
            .OrderBy(d => d.Name)
            .Select(d => new DistrictDto(d.Id, d.Name, d.ProvinceId))
            .ToListAsync();
        return Ok(new ListResponse<DistrictDto>(items.Count, items));
    }
}