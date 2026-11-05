using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Mapping;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public record CreateReviewCommand(
    long ProductId,
    string ProductName,
    string? BrandName,
    string AuthorId,
    string AuthorName,
    int Rating,
    string? Title,
    string? Text,
    ScentProfileDto? ScentProfile,
    List<string>? Photos) : ICommand<string>;

public class CreateReviewCommandHandler(IReviewRepository reviews, ILogger<CreateReviewCommandHandler> logger)
    : IRequestHandler<CreateReviewCommand, string>
{
    public async Task<string> Handle(CreateReviewCommand cmd, CancellationToken ct)
    {
        var review = new Review(
            cmd.ProductId,
            cmd.ProductName,
            cmd.BrandName,
            new Author(cmd.AuthorId, cmd.AuthorName),
            cmd.Rating,
            cmd.Title,
            cmd.Text,
            verifiedPurchase: false,
            scentProfile: cmd.ScentProfile.ToDomain(),
            photos: cmd.Photos);

        await reviews.AddAsync(review, ct);

        logger.LogInformation("Review {ReviewId} created for product {ProductId} with rating {Rating}",
            review.Id.ToString(), cmd.ProductId, cmd.Rating);

        return review.Id.ToString();
    }
}