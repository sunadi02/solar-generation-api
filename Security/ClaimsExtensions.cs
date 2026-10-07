using System.Security.Claims;

namespace SolarGenerationApi.Security;

public static class ClaimsExtensions
{
    public static bool HasScope(this ClaimsPrincipal user, string scope)
    {
        var value = user.FindFirst("scope")?.Value ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope);
    }

    public static bool IsNational(this ClaimsPrincipal user)
    {
        var role = user.FindFirst("role")?.Value;
        return role == "Admin" || role == "National";
    }

    public static int? GetProvinceId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirst("province_id")?.Value, out var id) ? id : null;

    public static int? GetDistrictId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirst("district_id")?.Value, out var id) ? id : null;
}