using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public record NoteDto(int Id, string Name, string Family);

public record NoteWithPerfumesDto(int Id, string Name, string Family, IReadOnlyList<PerfumeListItemDto> Perfumes);

public class SaveNoteDto
{
    [Required, StringLength(60)]
    public string Name { get; set; } = "";

    [Required]
    public string Family { get; set; } = "";
}