using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);

    Task<(IReadOnlyList<OrderSummary> Items, int TotalCount)> GetByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct = default);

    Task AddItemAsync(long orderId, long productId, int units, CancellationToken ct = default);

    Task ConfirmAsync(long orderId, string? changedBy, CancellationToken ct = default);

    Task ChangeStatusAsync(long orderId, string newStatus, string? changedBy, CancellationToken ct = default);
}