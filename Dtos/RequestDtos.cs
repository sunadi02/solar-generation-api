using System.ComponentModel.DataAnnotations;

namespace SolarGenerationApi.Dtos;

public class CreateReadingRequest
{
    [Required] public DateTimeOffset? Timestamp { get; set; }
    [Required, Range(0, 1000)] public double? InstantaneousPowerKw { get; set; }
    [Required, Range(0, double.MaxValue)] public double? CumulativeEnergyKwh { get; set; }
    [Required, Range(0, 1000)] public double? Voltage { get; set; }
}

public class CreateInstallationRequest
{
    [Required, StringLength(50)] public string MeterIdentifier { get; set; } = string.Empty;
    [Required, StringLength(200)] public string OwnerName { get; set; } = string.Empty;
    [Required, StringLength(300)] public string Address { get; set; } = string.Empty;
    [Required, Range(0.1, 1000)] public double? CapacityKw { get; set; }
    [Required] public DateTime? InstalledOn { get; set; }
    [Required] public int? GridSubstationId { get; set; }
    [Required, StringLength(100, MinimumLength = 8)] public string DeviceSecret { get; set; } = string.Empty;
}

public class ReplaceInstallationRequest
{
    [Required, StringLength(50)] public string MeterIdentifier { get; set; } = string.Empty;
    [Required, StringLength(200)] public string OwnerName { get; set; } = string.Empty;
    [Required, StringLength(300)] public string Address { get; set; } = string.Empty;
    [Required, Range(0.1, 1000)] public double? CapacityKw { get; set; }
    [Required] public DateTime? InstalledOn { get; set; }
    [Required] public int? GridSubstationId { get; set; }
}

public class PatchInstallationRequest
{
    [StringLength(50, MinimumLength = 1)] public string? MeterIdentifier { get; set; }
    [StringLength(200, MinimumLength = 1)] public string? OwnerName { get; set; }
    [StringLength(300, MinimumLength = 1)] public string? Address { get; set; }
    [Range(0.1, 1000)] public double? CapacityKw { get; set; }
    public DateTime? InstalledOn { get; set; }
    public int? GridSubstationId { get; set; }
}