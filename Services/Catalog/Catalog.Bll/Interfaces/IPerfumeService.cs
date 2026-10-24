using Catalog.Bll.Dtos;
using Catalog.Bll.Queries;

namespace Catalog.Bll.Interfaces;

public interface IPerfumeService
{
    Task<PagedResultDto<PerfumeListItemDto>> GetListAsync(PerfumeQuery query, CancellationToken ct);
    Task<PerfumeDto> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<PerfumeListItemDto>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<IReadOnlyList<PerfumeListItemDto>> GetByNoteAsync(string noteName, string? layer, CancellationToken ct);
    Task<PerfumeDto> CreateAsync(CreatePerfumeDto input, CancellationToken ct);
    Task<PerfumeDto> UpdateAsync(int id, UpdatePerfumeDto input, CancellationToken ct);
    Task<PerfumeDto> ChangePriceAsync(int id, ChangePriceDto input, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}