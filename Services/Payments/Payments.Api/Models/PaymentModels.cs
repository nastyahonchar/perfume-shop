using System.ComponentModel.DataAnnotations;

namespace Payments.Api.Models;

public static class PaymentStatus
{
    public const string Processed = "Processed";
    public const string Refunded = "Refunded";
}

public record PaymentDto(Guid Id, long OrderId, decimal Amount, string Currency, string Status, DateTime CreatedAt);

public class ProcessPaymentRequest
{
    [Range(1, long.MaxValue)]
    public long OrderId { get; set; }

    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "";
}

public class RefundPaymentRequest
{
    public Guid PaymentId { get; set; }
}
