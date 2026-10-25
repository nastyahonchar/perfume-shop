using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class PerfumeNoteRepository : GenericRepository<PerfumeNote>, IPerfumeNoteRepository
{
    public PerfumeNoteRepository(CatalogDbContext context) : base(context) { }

    public override Task<PerfumeNote?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new NotSupportedException("PerfumeNote has a composite key");

    public async Task<IReadOnlyList<Perfume>> GetPerfumesByNoteAsync(
        string noteName, string? layer = null, CancellationToken ct = default)
    {
        var perfumeIds = Set
            .Where(pn => pn.Note.Name == noteName && (layer == null || pn.Layer == layer))
            .Select(pn => pn.PerfumeId);

        return await Context.Set<Perfume>()
            .Include(p => p.Brand)
            .Where(p => p.IsActive && !p.IsDeleted && perfumeIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Perfume>> GetPerfumesByAllNotesAsync(
        IReadOnlyCollection<string> noteNames, CancellationToken ct = default)
    {
        var names = noteNames.Distinct().ToList();
        if (names.Count == 0)
            return Array.Empty<Perfume>();

        var perfumeIds = Set
            .Where(pn => names.Contains(pn.Note.Name))
            .GroupBy(pn => pn.PerfumeId)
            .Where(g => g.Count() == names.Count)
            .Select(g => g.Key);

        return await Context.Set<Perfume>()
            .Include(p => p.Brand)
            .Where(p => p.IsActive && !p.IsDeleted && perfumeIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}