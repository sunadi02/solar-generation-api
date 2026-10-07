using System.Security.Claims;
using SolarGenerationApi.Data;
using SolarGenerationApi.Models;
using SolarGenerationApi.Security;

namespace SolarGenerationApi.Services;

public class JurisdictionService
{
    private readonly SolarGenerationDbContext _db;

    public JurisdictionService(SolarGenerationDbContext db)
    {
        _db = db;
    }

    public IQueryable<Province> Provinces(ClaimsPrincipal user)
    {
        var q = _db.Provinces.AsQueryable();
        if (user.IsNational()) return q;
        var pid = user.GetProvinceId();
        var did = user.GetDistrictId();
        if (pid.HasValue) return q.Where(p => p.Id == pid.Value);
        if (did.HasValue) return q.Where(p => p.Districts.Any(d => d.Id == did.Value));
        return q.Where(p => false);
    }

    public IQueryable<District> Districts(ClaimsPrincipal user)
    {
        var q = _db.Districts.AsQueryable();
        if (user.IsNational()) return q;
        var pid = user.GetProvinceId();
        var did = user.GetDistrictId();
        if (pid.HasValue) return q.Where(d => d.ProvinceId == pid.Value);
        if (did.HasValue) return q.Where(d => d.Id == did.Value);
        return q.Where(d => false);
    }

    public IQueryable<GridSubstation> GridSubstations(ClaimsPrincipal user)
    {
        var q = _db.GridSubstations.AsQueryable();
        if (user.IsNational()) return q;
        var pid = user.GetProvinceId();
        var did = user.GetDistrictId();
        if (pid.HasValue) return q.Where(s => s.District!.ProvinceId == pid.Value);
        if (did.HasValue) return q.Where(s => s.DistrictId == did.Value);
        return q.Where(s => false);
    }
}