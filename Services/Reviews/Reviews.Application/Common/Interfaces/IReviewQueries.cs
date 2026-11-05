using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDetailsDto?> GetByIdAsync(string id, CancellationToken ct);

    Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId, int page, int pageSize, CancellationToken ct);
}