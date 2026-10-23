namespace Catalog.Domain.Entities;

public class Note
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Family { get; set; } = "";

    public List<PerfumeNote> PerfumeNotes { get; } = new();
}