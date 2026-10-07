using System.ComponentModel.DataAnnotations;

namespace SolarGenerationApi.Dtos;

public class UserLoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class DeviceLoginRequest
{
    [Required] public string MeterIdentifier { get; set; } = string.Empty;
    [Required] public string DeviceSecret { get; set; } = string.Empty;
}

public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);