using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IPerfumeRepository : IGenericRepository<Perfume>
{
    Task<Perfume?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
    Task<bool> ExistsByBrandNameVolumeAsync(int brandId, string name, short volumeMl, CancellationToken ct = default);
}