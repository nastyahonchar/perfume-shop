using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Mapping;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public record UpdateReviewCommand(
    string Id,
    int Version,
    int Rating,
    string? Title,
    string? Text,
    ScentProfileDto? ScentProfile) : ICommand<ReviewDetailsDto>;

public class UpdateReviewCommandHandler(IReviewRepository reviews, IMapper mapper)
    : IRequestHandler<UpdateReviewCommand, ReviewDetailsDto>
{
    public async Task<ReviewDetailsDto> Handle(UpdateReviewCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
                     ?? throw new NotFoundException($"Review {cmd.Id} not found");

        if (review.Version != cmd.Version)
            throw new ConflictException(
                $"Review {cmd.Id} was modified (expected version {cmd.Version}, current {review.Version})");

        review.Update(cmd.Rating, cmd.Title, cmd.Text, cmd.ScentProfile.ToDomain());

        await reviews.UpdateAsync(review, ct);

        return mapper.Map<ReviewDetailsDto>(review);
    }
}