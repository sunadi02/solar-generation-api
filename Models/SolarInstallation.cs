namespace SolarGenerationApi.Models;

public class SolarInstallation
{
    public int Id { get; set; }
    public string MeterIdentifier { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double CapacityKw { get; set; }
    public DateTime InstalledOn { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DeviceSecretHash { get; set; } = string.Empty;
    public int GridSubstationId { get; set; }
    public GridSubstation? GridSubstation { get; set; }
    public ICollection<GenerationReading> GenerationReadings { get; set; } = new List<GenerationReading>();
}