namespace Catalog.Domain.Entities;

public class PerfumeDetail
{
    public int PerfumeId { get; set; }
    public string Description { get; set; } = "";
    public string? Perfumer { get; set; }
    public short? LaunchYear { get; set; }
    public byte? Longevity { get; set; }
}