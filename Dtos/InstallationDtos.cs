namespace SolarGenerationApi.Dtos;

public record InstallationDto(int Id, string MeterIdentifier, string OwnerName, string Address,
    double CapacityKw, DateTime InstalledOn, DateTime UpdatedAt, int GridSubstationId);

public record ReadingDto(long Id, int SolarInstallationId, DateTime Timestamp,
    double InstantaneousPowerKw, double CumulativeEnergyKwh, double Voltage);

public record LocationDto(string Province, string District, string GridSubstation, string GridSubstationCode);

public record InstallationOverviewDto(InstallationDto Installation, LocationDto Location, ReadingDto? LastReading);

public record PageLinks(string Self, string First, string? Previous, string? Next, string Last);

public record PagedResponse<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items, PageLinks Links);