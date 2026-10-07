using System.Security.Claims;

namespace SolarGenerationApi.Security;

public static class ClaimsExtensions
{
    public static bool HasScope(this ClaimsPrincipal user, string scope)
    {
        var value = user.FindFirst("scope")?.Value ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope);
    }
}