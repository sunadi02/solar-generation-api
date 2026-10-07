using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SolarGenerationApi.Models;

namespace SolarGenerationApi.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, int ExpiresIn) CreateUserToken(User user)
    {
        var scopes = user.Role == "Admin" ? "readings:read admin:write" : "readings:read";
        var claims = new List<Claim>
        {
            new("uid", user.Id.ToString()),
            new("role", user.Role),
            new("scope", scopes)
        };
        if (user.ProvinceId.HasValue) claims.Add(new Claim("province_id", user.ProvinceId.Value.ToString()));
        if (user.DistrictId.HasValue) claims.Add(new Claim("district_id", user.DistrictId.Value.ToString()));
        return Build(claims, 3600);
    }

    public (string Token, int ExpiresIn) CreateDeviceToken(SolarInstallation installation)
    {
        var claims = new List<Claim>
        {
            new("installation_id", installation.Id.ToString()),
            new("scope", "installation:write")
        };
        return Build(claims, 3600);
    }

    private (string Token, int ExpiresIn) Build(IEnumerable<Claim> claims, int seconds)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(seconds),
            signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), seconds);
    }
}