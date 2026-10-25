using Catalog.Bll.Dtos;

namespace Catalog.Bll.Interfaces;

public interface INoteService
{
    Task<IReadOnlyList<NoteDto>> GetAllAsync(string? family, CancellationToken ct);
    Task<NoteDto> GetByIdAsync(int id, CancellationToken ct);
    Task<NoteWithPerfumesDto> GetWithPerfumesAsync(int id, CancellationToken ct);
    Task<NoteDto> CreateAsync(SaveNoteDto input, CancellationToken ct);
}