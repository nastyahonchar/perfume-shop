using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Exceptions;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public record GetReviewByIdQuery(string Id) : IQuery<ReviewDetailsDto>;

public class GetReviewByIdQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewByIdQuery, ReviewDetailsDto>
{
    public async Task<ReviewDetailsDto> Handle(GetReviewByIdQuery q, CancellationToken ct)
        => await queries.GetByIdAsync(q.Id, ct)
           ?? throw new NotFoundException($"Review {q.Id} not found");
}