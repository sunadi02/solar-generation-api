namespace SolarGenerationApi.Models;

public class District
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ProvinceId { get; set; }

    public Province? Province { get; set; }

    public ICollection<GridSubstation> GridSubstations { get; set; } = new List<GridSubstation>();
}