using Reviews.Application.Reviews.Dtos;

namespace Reviews.WebApi.Contracts;

public record UpdateReviewRequest(
    int Rating,
    string? Title,
    string? Text,
    ScentProfileDto? ScentProfile,
    int? Version);

public record AddCommentRequest(string AuthorId, string AuthorName, string Text);