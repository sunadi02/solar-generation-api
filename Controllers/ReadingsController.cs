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
[Route("api/v1/readings")]
public class ReadingsController : ControllerBase
{
    private readonly SolarGenerationDbContext _db;
    private readonly JurisdictionService _scope;

    public ReadingsController(SolarGenerationDbContext db, JurisdictionService scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        int? province, int? district, int? substation, int? installation,
        [FromQuery(Name = "from")] DateTimeOffset? fromTime,
        [FromQuery(Name = "to")] DateTimeOffset? toTime,
        string? sort,
        int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return Err.Make(400, "VALIDATION_ERROR", "page must be at least 1 and page_size must be between 1 and 100.");
        if (fromTime.HasValue && toTime.HasValue && fromTime > toTime)
            return Err.Make(400, "VALIDATION_ERROR", "'from' must not be later than 'to'.");
        if (sort != null && sort != "timestamp" && sort != "-timestamp")
            return Err.Make(400, "VALIDATION_ERROR", "sort must be timestamp or -timestamp.");

      
        var allowed = _scope.Installations(User);
        var q = _db.GenerationReadings.AsNoTracking()
            .Where(r => allowed.Any(s => s.Id == r.SolarInstallationId));

        if (province.HasValue)
            q = q.Where(r => r.SolarInstallation!.GridSubstation!.District!.ProvinceId == province.Value);
        if (district.HasValue)
            q = q.Where(r => r.SolarInstallation!.GridSubstation!.DistrictId == district.Value);
        if (substation.HasValue)
            q = q.Where(r => r.SolarInstallation!.GridSubstationId == substation.Value);
        if (installation.HasValue)
            q = q.Where(r => r.SolarInstallationId == installation.Value);
        if (fromTime.HasValue)
        {
            var f = fromTime.Value.UtcDateTime;
            q = q.Where(r => r.Timestamp >= f);
        }
        if (toTime.HasValue)
        {
            var t = toTime.Value.UtcDateTime;
            q = q.Where(r => r.Timestamp <= t);
        }

        var total = await q.CountAsync();
        var ordered = sort == "timestamp"
            ? q.OrderBy(r => r.Timestamp)
            : q.OrderByDescending(r => r.Timestamp);

        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ReadingDto(r.Id, r.SolarInstallationId, r.Timestamp,
                r.InstantaneousPowerKw, r.CumulativeEnergyKwh, r.Voltage))
            .ToListAsync();

        return Ok(new PagedResponse<ReadingDto>(total, page, pageSize, items,
            PagingLinks.Build(Request, page, pageSize, total)));
    }
}