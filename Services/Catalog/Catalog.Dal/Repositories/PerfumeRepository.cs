using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class PerfumeRepository : GenericRepository<Perfume>, IPerfumeRepository
{
    public PerfumeRepository(CatalogDbContext context) : base(context) { }

    public Task<Perfume?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.Include(p => p.Brand)
              .Include(p => p.Detail)
              .Include(p => p.Images)
              .Include(p => p.PerfumeNotes).ThenInclude(pn => pn.Note)
              .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
        => Set.AnyAsync(p => p.Sku == sku, ct);

    public Task<bool> ExistsByBrandNameVolumeAsync(int brandId, string name, short volumeMl, CancellationToken ct = default)
        => Set.AnyAsync(p => p.BrandId == brandId && p.Name == name && p.VolumeMl == volumeMl, ct);
}