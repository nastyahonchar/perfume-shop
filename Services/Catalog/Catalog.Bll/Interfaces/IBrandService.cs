using Catalog.Bll.Dtos;

namespace Catalog.Bll.Interfaces;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct);
    Task<BrandDto> GetByIdAsync(int id, CancellationToken ct);
    Task<BrandDto> CreateAsync(SaveBrandDto input, CancellationToken ct);
    Task<BrandDto> UpdateAsync(int id, SaveBrandDto input, CancellationToken ct);
}