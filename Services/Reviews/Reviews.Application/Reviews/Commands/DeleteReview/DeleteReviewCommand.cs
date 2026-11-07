using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Common.Messaging;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public record DeleteReviewCommand(string Id) : ICommand;

public class DeleteReviewCommandHandler(IReviewRepository reviews, ILogger<DeleteReviewCommandHandler> logger)
    : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand cmd, CancellationToken ct)
    {
        var deleted = await reviews.DeleteAsync(ObjectId.Parse(cmd.Id), ct);
        if (!deleted)
            throw new NotFoundException($"Review {cmd.Id} not found");

        logger.LogInformation("Review {ReviewId} deleted", cmd.Id);
    }
}