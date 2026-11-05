namespace Reviews.Application.Reviews.Dtos;

public record AuthorDto(string UserId, string DisplayName);

public record NoteRatingDto(string Note, int Score);

public record ScentProfileDto(
    double? LongevityHours,
    string? Sillage,
    IReadOnlyList<string>? SeasonsOfWear,
    IReadOnlyList<NoteRatingDto>? NotesRated);

public record CommentDto(string Id, AuthorDto Author, string Text, DateTime CreatedAt);

public record ReviewDto(
    string Id,
    long ProductId,
    string ProductName,
    string? BrandName,
    string AuthorName,
    int Rating,
    string? Title,
    string? Text,
    bool VerifiedPurchase,
    int CommentsCount,
    DateTime CreatedAt,
    int Version);

public record ReviewDetailsDto(
    string Id,
    long ProductId,
    string ProductName,
    string? BrandName,
    AuthorDto Author,
    int Rating,
    string? Title,
    string? Text,
    bool VerifiedPurchase,
    IReadOnlyList<string> Photos,
    ScentProfileDto? ScentProfile,
    IReadOnlyList<CommentDto> Comments,
    string? DiscussionId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);