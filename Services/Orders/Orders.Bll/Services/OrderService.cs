using System.Data;
using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Orders.Bll.Dtos;
using Orders.Bll.Interfaces;
using Orders.Dal.Interfaces;
using Orders.Domain.Constants;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public class OrderService : IOrderService
{
    private const string ChangedBy = "orders-api";

    private static readonly ActivitySource Source = new("Orders.Bll");

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork uow, IMapper mapper, ILogger<OrderService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct)
                    ?? throw new NotFoundException($"Order {id} not found");
        return _mapper.Map<OrderDto>(order);
    }

    public async Task<PagedResultDto<OrderSummaryDto>> GetByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct)
    {
        if (status is not null && !OrderStatus.All.Contains(status))
            throw new ValidationException($"Unknown status '{status}'");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (await _uow.Customers.GetByIdAsync(customerId, ct) is null)
            throw new NotFoundException($"Customer {customerId} not found");

        var (items, total) = await _uow.Orders.GetByCustomerAsync(customerId, status, page, pageSize, ct);
        return new PagedResultDto<OrderSummaryDto>(
            items.Select(i => _mapper.Map<OrderSummaryDto>(i)).ToList(), total, page, pageSize);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct)
    {
        using var activity = Source.StartActivity("CreateOrder");
        activity?.SetTag("customer.id", input.CustomerId);

        var order = await ExecuteInTransactionAsync(async () =>
        {
            var customer = await _uow.Customers.GetByIdAsync(input.CustomerId, ct)
                           ?? throw new NotFoundException($"Customer {input.CustomerId} not found");

            var orderId = await _uow.Orders.AddAsync(new Order
            {
                CustomerId = customer.Id,
                ShippingAddress = input.ShippingAddress.Trim(),
                Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim()
            }, ct);

            foreach (var item in input.Items)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId, ct)
                              ?? throw new NotFoundException($"Product {item.ProductId} not found");

                if (!product.IsActive)
                    throw new BusinessConflictException($"Product {product.Id} is not available");

                await _uow.Orders.AddItemAsync(orderId, product.Id, item.Units, ct);
            }

            var created = await _uow.Orders.GetWithItemsAsync(orderId, ct)
                          ?? throw new NotFoundException($"Order {orderId} not found");
            return _mapper.Map<OrderDto>(created);
        }, ct);

        activity?.SetTag("order.id", order.Id);
        _logger.LogInformation(
            "Order {OrderId} created for customer {CustomerId}: {ItemsCount} items, total {TotalAmount}, status {Status}",
            order.Id, order.CustomerId, order.Items.Count, order.TotalAmount, order.Status);

        return order;
    }

    public async Task<OrderDto> UpdateDetailsAsync(long id, UpdateOrderDto input, CancellationToken ct)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct)
                    ?? throw new NotFoundException($"Order {id} not found");

        if (order.Status != OrderStatus.Submitted)
            throw new BusinessConflictException("Order details can be changed only while the order is Submitted");

        order.ShippingAddress = input.ShippingAddress.Trim();
        order.Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim();

        await _uow.Orders.UpdateAsync(order, ct);

        _logger.LogInformation("Order {OrderId} details updated, status {Status}", id, order.Status);
        return _mapper.Map<OrderDto>(order);
    }

    public async Task ConfirmOrderAsync(long orderId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("ConfirmOrder");
        activity?.SetTag("order.id", orderId);

        var itemsCount = 0;

        await ExecuteInTransactionAsync(async () =>
        {
            var order = await _uow.Orders.GetWithItemsAsync(orderId, ct)
                        ?? throw new NotFoundException($"Order {orderId} not found");

            if (order.Items.Count == 0)
                throw new BusinessConflictException($"Order {orderId} has no items");

            itemsCount = order.Items.Count;
            await _uow.Orders.ConfirmAsync(orderId, ChangedBy, ct);
        }, ct);

        _logger.LogInformation("Order {OrderId} confirmed, {ItemsCount} items, status {Status}",
            orderId, itemsCount, OrderStatus.StockConfirmed);
    }

    public async Task ChangeStatusAsync(long orderId, string newStatus, CancellationToken ct)
    {
        if (!OrderStatus.All.Contains(newStatus))
            throw new ValidationException($"Unknown status '{newStatus}'");

        if (newStatus == OrderStatus.StockConfirmed)
        {
            await ConfirmOrderAsync(orderId, ct);
            return;
        }

        using var activity = Source.StartActivity("ChangeOrderStatus");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("order.status", newStatus);

        await ExecuteInTransactionAsync(
            () => _uow.Orders.ChangeStatusAsync(orderId, newStatus, ChangedBy, ct), ct);

        _logger.LogInformation("Order {OrderId} status changed to {Status}", orderId, newStatus);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct)
                    ?? throw new NotFoundException($"Order {id} not found");

        if (order.Status is not (OrderStatus.Submitted or OrderStatus.Cancelled))
            throw new BusinessConflictException("Only a Submitted or Cancelled order can be deleted");

        await _uow.Orders.DeleteAsync(id, ct);

        _logger.LogInformation("Order {OrderId} deleted, status was {Status}", id, order.Status);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var result = await action();
            await _uow.CommitAsync(ct);
            return result;
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await action();
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}