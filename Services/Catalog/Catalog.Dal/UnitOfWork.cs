using Catalog.Dal.Interfaces;
using Catalog.Dal.Repositories;
using Catalog.Domain.Entities;

namespace Catalog.Dal;

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
        Brands = new GenericRepository<Brand>(context);
        Perfumes = new PerfumeRepository(context);
        Notes = new NoteRepository(context);
        PerfumeNotes = new PerfumeNoteRepository(context);
    }

    public IGenericRepository<Brand> Brands { get; }
    public IPerfumeRepository Perfumes { get; }
    public INoteRepository Notes { get; }
    public IPerfumeNoteRepository PerfumeNotes { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}