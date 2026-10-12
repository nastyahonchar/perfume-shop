using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetBySeasonAsync(string season, CancellationToken ct = default);
}