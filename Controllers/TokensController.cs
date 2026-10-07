using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGenerationApi.Data;
using SolarGenerationApi.Dtos;
using SolarGenerationApi.Errors;
using SolarGenerationApi.Services;

namespace SolarGenerationApi.Controllers;

[ApiController]
[Route("api/v1/tokens")]
public class TokensController : ControllerBase
{
    private readonly SolarGenerationDbContext _db;
    private readonly TokenService _tokens;

    public TokensController(SolarGenerationDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("user")]
    public async Task<IActionResult> CreateUserToken([FromBody] UserLoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Err.Make(401, "INVALID_CREDENTIALS", "Username or password is incorrect.");

        var (token, expiresIn) = _tokens.CreateUserToken(user);
        return Ok(new TokenResponse(token, "Bearer", expiresIn));
    }

    [HttpPost("device")]
    public async Task<IActionResult> CreateDeviceToken([FromBody] DeviceLoginRequest request)
    {
        var installation = await _db.SolarInstallations
            .FirstOrDefaultAsync(s => s.MeterIdentifier == request.MeterIdentifier);
        if (installation is null || !BCrypt.Net.BCrypt.Verify(request.DeviceSecret, installation.DeviceSecretHash))
            return Err.Make(401, "INVALID_CREDENTIALS", "Meter identifier or device secret is incorrect.");

        var (token, expiresIn) = _tokens.CreateDeviceToken(installation);
        return Ok(new TokenResponse(token, "Bearer", expiresIn));
    }
}