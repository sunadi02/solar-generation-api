namespace SolarGenerationApi.Models;

public class GridSubstation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int DistrictId { get; set; }

    public District? District { get; set; }

    public ICollection<SolarInstallation> SolarInstallations { get; set; } = new List<SolarInstallation>();
}