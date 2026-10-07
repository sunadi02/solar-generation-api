using Microsoft.AspNetCore.Mvc;

namespace SolarGenerationApi.Errors;

public record ApiError(string Code, string Message, object? Details = null);

public static class Err
{
    public static ObjectResult Make(int status, string code, string message, object? details = null)
        => new ObjectResult(new ApiError(code, message, details)) { StatusCode = status };

    public static ObjectResult NotFound(string what)
        => Make(404, "NOT_FOUND", $"{what} was not found.");

    public static ObjectResult OutOfJurisdiction()
        => Make(403, "OUT_OF_JURISDICTION", "This resource is outside your jurisdiction.");
}

