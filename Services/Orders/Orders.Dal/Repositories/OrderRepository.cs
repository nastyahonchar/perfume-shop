using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    public OrderRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Orders") { }

    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.OrderDate, d.ShippingAddress, d.Comment,
                   i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Discount, i.Units
            FROM dbo.Orders o
            JOIN dbo.OrderDetails d ON d.OrderId = o.Id
            LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @OrderId AND o.IsDeleted = 0
            ORDER BY i.ProductId";

        await EnsureOpenAsync(ct);

        Order? result = null;
        await Connection.QueryAsync<Order, OrderItem?, Order>(
            new CommandDefinition(sql, new { OrderId = orderId }, Transaction, cancellationToken: ct),
            (order, item) =>              
            {
                result ??= order;
                if (item is not null) result.Items.Add(item);
                return result;
            },
            splitOn: "OrderId");          
        return result;
    }

    public override Task<Order?> GetByIdAsync(long id, CancellationToken ct = default)
        => GetWithItemsAsync(id, ct);

    public override async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.OrderDate, d.ShippingAddress, d.Comment
            FROM dbo.Orders o
            JOIN dbo.OrderDetails d ON d.OrderId = o.Id
            WHERE o.IsDeleted = 0
            ORDER BY o.Id DESC";

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Order>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
        return rows.ToList();
    }

    public override async Task<long> AddAsync(Order entity, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CustomerId", entity.CustomerId, DbType.Int64);
        parameters.Add("@ShippingAddress", entity.ShippingAddress, DbType.String);
        parameters.Add("@Comment", entity.Comment, DbType.String);
        parameters.Add("@CreatedBy", "orders-api", DbType.String);
        parameters.Add("@OrderId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await EnsureOpenAsync(ct);
        await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_CreateOrder", parameters, Transaction,
            commandType: CommandType.StoredProcedure, cancellationToken: ct)));

        return parameters.Get<long>("@OrderId");
    }

    public override async Task<bool> UpdateAsync(Order entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.OrderDetails SET ShippingAddress = @ShippingAddress, Comment = @Comment WHERE OrderId = @Id",
            new { entity.Id, entity.ShippingAddress, entity.Comment }, Transaction, cancellationToken: ct));

        if (rows > 0)
            await Connection.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Orders SET UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                new { entity.Id }, Transaction, cancellationToken: ct));

        return rows > 0;
    }

    public override async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Orders SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id }, Transaction, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<(IReadOnlyList<OrderSummary> Items, int TotalCount)> GetByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = (await Connection.QueryAsync<OrderSummaryRow>(new CommandDefinition(
            "dbo.usp_GetCustomerOrders",
            new { CustomerId = customerId, Status = status, Page = page, PageSize = pageSize },
            Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct))).ToList();

        var items = rows.Select(r => new OrderSummary
        {
            Id = r.Id,
            CustomerId = customerId,
            Status = r.Status,
            OrderDate = r.OrderDate,
            TotalAmount = r.TotalAmount
        }).ToList();

        return (items, rows.Count > 0 ? rows[0].TotalCount : 0);
    }

    public async Task AddItemAsync(long orderId, long productId, int units, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_AddOrderItem",
            new { OrderId = orderId, ProductId = productId, Units = units },
            Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct)));
    }

    public async Task ConfirmAsync(long orderId, string? changedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_ConfirmOrder",
            new { OrderId = orderId, ChangedBy = changedBy },
            Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct)));
    }

    public async Task ChangeStatusAsync(
        long orderId, string newStatus, string? changedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        await TranslateAsync(() => Connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_ChangeOrderStatus",
            new { OrderId = orderId, NewStatus = newStatus, ChangedBy = changedBy },
            Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct)));
    }
}

internal sealed class OrderSummaryRow
{
    public long Id { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
}