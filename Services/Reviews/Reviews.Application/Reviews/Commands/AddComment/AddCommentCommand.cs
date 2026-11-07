using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.AddComment;

public record AddCommentCommand(string ReviewId, string AuthorId, string AuthorName, string Text)
    : ICommand<ReviewDetailsDto>;

public class AddCommentCommandHandler(
    IReviewRepository reviews, IMapper mapper, ILogger<AddCommentCommandHandler> logger)
    : IRequestHandler<AddCommentCommand, ReviewDetailsDto>
{
    public async Task<ReviewDetailsDto> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.ReviewId), ct)
                     ?? throw new NotFoundException($"Review {cmd.ReviewId} not found");

        review.AddComment(new Author(cmd.AuthorId, cmd.AuthorName), cmd.Text);

        await reviews.UpdateAsync(review, ct);

        logger.LogInformation("Comment added to review {ReviewId}, comments total {CommentsCount}",
            cmd.ReviewId, review.Comments.Count);

        return mapper.Map<ReviewDetailsDto>(review);
    }
}