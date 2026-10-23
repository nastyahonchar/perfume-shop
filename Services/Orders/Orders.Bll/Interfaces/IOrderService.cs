using Orders.Bll.Dtos;

namespace Orders.Bll.Interfaces;

public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(long id, CancellationToken ct);
    Task<PagedResultDto<OrderSummaryDto>> GetByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct);
    Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct);
    Task<OrderDto> UpdateDetailsAsync(long id, UpdateOrderDto input, CancellationToken ct);
    Task ConfirmOrderAsync(long orderId, CancellationToken ct);
    Task ChangeStatusAsync(long orderId, string newStatus, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
}