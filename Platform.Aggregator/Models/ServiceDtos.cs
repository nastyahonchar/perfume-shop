namespace Platform.Aggregator.Models;

public record OrderItemDto(long ProductId, string ProductName, decimal UnitPrice, decimal Discount, int Units);

public record OrderDto(
    long Id,
    long CustomerId,
    string Status,
    DateTime OrderDate,
    string ShippingAddress,
    string? Comment,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items);

public record ProductDto(
    int Id,
    string Brand,
    string Name,
    string Sku,
    string Gender,
    string Concentration,
    string Season,
    short VolumeMl,
    decimal Price,
    bool IsActive);

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

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);