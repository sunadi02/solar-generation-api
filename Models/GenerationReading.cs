namespace SolarGenerationApi.Models;

public class GenerationReading
{
    public long Id { get; set; }

    public int SolarInstallationId { get; set; }

    public SolarInstallation? SolarInstallation { get; set; }

    public DateTime Timestamp { get; set; }

    public double InstantaneousPowerKw { get; set; }

    public double CumulativeEnergyKwh { get; set; }

    public double Voltage { get; set; }
}