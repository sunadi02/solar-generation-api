namespace SolarGenerationApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    // admin,national,provincial,district
    public string Role { get; set; } = string.Empty;
    public int? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public int? DistrictId { get; set; }
    public District? District { get; set; }
}