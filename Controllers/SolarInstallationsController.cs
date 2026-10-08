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

        private Task<ReadingDto?> LastReading(int id) =>
        _db.GenerationReadings.AsNoTracking()
            .Where(r => r.SolarInstallationId == id)
            .OrderByDescending(r => r.Timestamp)
            .Select(ToReadingDto)
            .FirstOrDefaultAsync();

    [HttpGet("{id:int}/last-reading")]
    [Authorize(Policy = "readings:read")]
    public async Task<IActionResult> GetLastReading(int id)
    {
        var denied = await CheckAccess(id);
        if (denied != null) return denied;

        var last = await LastReading(id);
        if (last is null) return Err.NotFound("Reading");
        return Ok(last);
    }

    [HttpGet("{id:int}/overview")]
    [Authorize(Policy = "readings:read")]
    public async Task<IActionResult> GetOverview(int id)
    {
        var denied = await CheckAccess(id);
        if (denied != null) return denied;

        var s = await _db.SolarInstallations.AsNoTracking()
            .Include(x => x.GridSubstation!).ThenInclude(g => g.District!).ThenInclude(d => d.Province!)
            .FirstAsync(x => x.Id == id);

        var installation = new InstallationDto(s.Id, s.MeterIdentifier, s.OwnerName, s.Address,
            s.CapacityKw, s.InstalledOn, s.UpdatedAt, s.GridSubstationId);
        var location = new LocationDto(
            s.GridSubstation!.District!.Province!.Name,
            s.GridSubstation.District.Name,
            s.GridSubstation.Name,
            s.GridSubstation.Code);

        return Ok(new InstallationOverviewDto(installation, location, await LastReading(id)));
    }

        [HttpGet("{id:int}/readings")]
    [Authorize(Policy = "readings:read")]
    public async Task<IActionResult> GetReadings(int id,
        [FromQuery(Name = "from")] DateTimeOffset? fromTime,
        [FromQuery(Name = "to")] DateTimeOffset? toTime,
        string? sort,
        int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        var denied = await CheckAccess(id);
        if (denied != null) return denied;

        var invalid = ValidatePaging(page, pageSize);
        if (invalid != null) return invalid;

        if (fromTime.HasValue && toTime.HasValue && fromTime > toTime)
            return Err.Make(400, "VALIDATION_ERROR", "'from' must not be later than 'to'.");
        if (sort != null && sort != "timestamp" && sort != "-timestamp")
            return Err.Make(400, "VALIDATION_ERROR", "sort must be timestamp or -timestamp.");

        var q = _db.GenerationReadings.AsNoTracking().Where(r => r.SolarInstallationId == id);
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

        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(ToReadingDto).ToListAsync();

        return Ok(new PagedResponse<ReadingDto>(total, page, pageSize, items,
            PagingLinks.Build(Request, page, pageSize, total)));
    }

        [HttpGet("{id:int}/readings/{readingId:long}")]
    [Authorize(Policy = "readings:read")]
    public async Task<IActionResult> GetReading(int id, long readingId)
    {
        var denied = await CheckAccess(id);
        if (denied != null) return denied;

        var dto = await _db.GenerationReadings.AsNoTracking()
            .Where(r => r.Id == readingId && r.SolarInstallationId == id)
            .Select(ToReadingDto)
            .FirstOrDefaultAsync();
        if (dto is null) return Err.NotFound("Reading");
        return Ok(dto);
    }

    [HttpPost("{id:int}/readings")]
    [Authorize(Policy = "installation:write")]
    public async Task<IActionResult> CreateReading(int id, [FromBody] CreateReadingRequest request)
    {
        //device token is only for their own installation
        var claim = User.FindFirst("installation_id")?.Value;
        if (claim != id.ToString())
            return Err.Make(403, "NOT_YOUR_INSTALLATION",
                "This token may only submit readings for its own installation.");

        var installation = await _db.SolarInstallations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (installation is null) return Err.NotFound("Solar installation");

        var timestamp = request.Timestamp!.Value.UtcDateTime;
        if (timestamp > DateTime.UtcNow.AddMinutes(5))
            return Err.Make(400, "VALIDATION_ERROR", "timestamp must not be in the future.");
        if (request.InstantaneousPowerKw!.Value > installation.CapacityKw * 1.25)
            return Err.Make(400, "VALIDATION_ERROR", "instantaneousPowerKw exceeds the installation's capacity.");

        if (await _db.GenerationReadings.AnyAsync(r => r.SolarInstallationId == id && r.Timestamp == timestamp))
            return Err.Make(409, "DUPLICATE_READING",
                "A reading for this installation and timestamp already exists.");

        var reading = new GenerationReading
        {
            SolarInstallationId = id,
            Timestamp = timestamp,
            InstantaneousPowerKw = request.InstantaneousPowerKw!.Value,
            CumulativeEnergyKwh = request.CumulativeEnergyKwh!.Value,
            Voltage = request.Voltage!.Value
        };
        _db.GenerationReadings.Add(reading);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Err.Make(409, "DUPLICATE_READING",
                "A reading for this installation and timestamp already exists.");
        }

        return CreatedAtAction(nameof(GetReading), new { id, readingId = reading.Id },
            new ReadingDto(reading.Id, reading.SolarInstallationId, reading.Timestamp,
                reading.InstantaneousPowerKw, reading.CumulativeEnergyKwh, reading.Voltage));
    }
}