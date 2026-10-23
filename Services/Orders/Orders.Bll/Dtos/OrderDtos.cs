using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.Dtos;

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

public record OrderSummaryDto(long Id, long CustomerId, string Status, DateTime OrderDate, decimal TotalAmount);

public class CreateOrderDto
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required, StringLength(400)]
    public string ShippingAddress { get; set; } = "";

    [StringLength(1000)]
    public string? Comment { get; set; }

    [Required, MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; set; }

    [Range(1, 1000)]
    public int Units { get; set; }
}

public class UpdateOrderDto
{
    [Required, StringLength(400)]
    public string ShippingAddress { get; set; } = "";

    [StringLength(1000)]
    public string? Comment { get; set; }
}