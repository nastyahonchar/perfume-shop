using System.Collections.Concurrent;
using Payments.Api.Models;

namespace Payments.Api.Services;

public sealed class PaymentStore
{
    private readonly ConcurrentDictionary<Guid, PaymentDto> _payments = new();
    private readonly object _refundLock = new();

    public IReadOnlyList<PaymentDto> GetAll()
        => _payments.Values.OrderByDescending(p => p.CreatedAt).ToList();

    public PaymentDto? Find(Guid id)
        => _payments.GetValueOrDefault(id);

    public PaymentDto Add(long orderId, decimal amount, string currency)
    {
        var payment = new PaymentDto(Guid.NewGuid(), orderId, amount, currency, PaymentStatus.Processed, DateTime.UtcNow);
        _payments[payment.Id] = payment;

        return payment;
    }

    public PaymentDto Refund(Guid id)
    {
        lock (_refundLock)
        {
            var refunded = _payments[id] with { Status = PaymentStatus.Refunded };
            _payments[id] = refunded;

            return refunded;
        }
    }
}
