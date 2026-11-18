using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetBySeasonAsync(string season, CancellationToken ct = default);

    Task<SnapshotUpdateResult> UpdatePriceSnapshotAsync(
        int productId, decimal price, DateTime sourceUpdatedAtUtc, CancellationToken ct = default);

    Task<SnapshotUpdateResult> UpdateNameSnapshotAsync(
        int productId, string name, DateTime sourceUpdatedAtUtc, CancellationToken ct = default);
}