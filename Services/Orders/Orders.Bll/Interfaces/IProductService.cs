using Orders.Bll.Dtos;

namespace Orders.Bll.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct);
    Task<ProductDto> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<ProductDto>> GetBySeasonAsync(string season, CancellationToken ct);
    Task<ProductDto> SetStockAsync(long id, int stock, CancellationToken ct);
}