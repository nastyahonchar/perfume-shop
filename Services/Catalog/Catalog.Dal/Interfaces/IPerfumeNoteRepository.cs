using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IPerfumeNoteRepository : IGenericRepository<PerfumeNote>
{
    Task<IReadOnlyList<Perfume>> GetPerfumesByNoteAsync(
        string noteName, string? layer = null, CancellationToken ct = default);

    Task<IReadOnlyList<Perfume>> GetPerfumesByAllNotesAsync(
        IReadOnlyCollection<string> noteNames, CancellationToken ct = default);
}