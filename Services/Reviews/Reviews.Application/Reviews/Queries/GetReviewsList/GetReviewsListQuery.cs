using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public record GetReviewsListQuery(long? ProductId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ReviewDto>>;

public class GetReviewsListQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    public Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery q, CancellationToken ct)
        => queries.GetListAsync(q.ProductId, Math.Max(q.Page, 1), Math.Clamp(q.PageSize, 1, 100), ct);
}