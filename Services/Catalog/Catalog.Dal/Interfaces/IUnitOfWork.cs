using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<Brand> Brands { get; }
    IPerfumeRepository Perfumes { get; }
    INoteRepository Notes { get; }
    IPerfumeNoteRepository PerfumeNotes { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}