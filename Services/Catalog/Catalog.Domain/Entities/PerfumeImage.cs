namespace Catalog.Domain.Entities;

public class PerfumeImage
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}