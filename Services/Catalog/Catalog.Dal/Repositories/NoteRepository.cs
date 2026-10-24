using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class NoteRepository : GenericRepository<Note>, INoteRepository
{
    public NoteRepository(CatalogDbContext context) : base(context) { }

    public async Task<Note?> GetWithPerfumesAsync(int id, CancellationToken ct = default)
    {
        var note = await Set.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null) return null;

        await Context.Entry(note)
            .Collection(n => n.PerfumeNotes)
            .Query()
            .Include(pn => pn.Perfume)
            .Where(pn => !pn.Perfume.IsDeleted)
            .LoadAsync(ct);

        return note;
    }

    public async Task<IReadOnlyList<Note>> GetByFamilyAsync(string family, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Where(n => n.Family == family)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => Set.AnyAsync(n => n.Name == name, ct);
}