using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface INoteRepository : IGenericRepository<Note>
{
    Task<Note?> GetWithPerfumesAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Note>> GetByFamilyAsync(string family, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}