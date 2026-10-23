namespace Catalog.Domain.Entities;

public class PerfumeNote
{
    public int PerfumeId { get; set; }
    public Perfume Perfume { get; set; } = null!;

    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public string Layer { get; set; } = "";
    public DateTime AddedAt { get; set; }
}