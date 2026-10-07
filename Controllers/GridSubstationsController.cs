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
[Route("api/v1/grid-substations")]
public class GridSubstationsController : ControllerBase
{
    private readonly SolarGenerationDbContext _db;
    private readonly JurisdictionService _scope;

    public GridSubstationsController(SolarGenerationDbContext db, JurisdictionService scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var s = await _db.GridSubstations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return Err.NotFound("Grid substation");
        if (!await _scope.GridSubstations(User).AnyAsync(x => x.Id == id)) return Err.OutOfJurisdiction();
        return Ok(new GridSubstationDto(s.Id, s.Name, s.Code, s.DistrictId));
    }
}