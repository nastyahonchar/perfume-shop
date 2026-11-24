using Reviews.Application.Reviews.Dtos;

namespace Reviews.WebApi.Contracts;

public record CreateReviewRequest(
    long ProductId,
    string ProductName,
    string? BrandName,
    int Rating,
    string? Title,
    string? Text,
    ScentProfileDto? ScentProfile,
    List<string>? Photos);

public record UpdateReviewRequest(
    int Rating,
    string? Title,
    string? Text,
    ScentProfileDto? ScentProfile,
    int? Version);

public record AddCommentRequest(string Text);