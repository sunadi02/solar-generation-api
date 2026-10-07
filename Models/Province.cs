namespace SolarGenerationApi.Models;

public class Province
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<District> Districts { get; set; } = new List<District>();
}