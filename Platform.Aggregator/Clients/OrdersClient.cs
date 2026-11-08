using System.Diagnostics;
using Grpc.Core;
using Orders.Grpc;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class OrdersClient(OrdersService.OrdersServiceClient grpc, ILogger<OrdersClient> logger)
{
    private const string ServiceName = "orders-api";

    public async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        logger.LogInformation("Calling {Service} gRPC GetOrder for order {OrderId}", ServiceName, orderId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var reply = await grpc.GetOrderAsync(
                new GetOrderRequest { Id = orderId },
                deadline: GrpcDefaults.Deadline(),
                cancellationToken: ct);

            logger.LogInformation("{Service} responded {GrpcStatus} in {Elapsed} ms",
                ServiceName, StatusCode.OK, stopwatch.ElapsedMilliseconds);

            return ToDto(reply);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
        {
            logger.LogInformation("{Service} responded {GrpcStatus} in {Elapsed} ms",
                ServiceName, ex.StatusCode, stopwatch.ElapsedMilliseconds);
            return null;
        }
        catch (RpcException ex)
        {
            logger.LogWarning("{Service} failed with {GrpcStatus} in {Elapsed} ms",
                ServiceName, ex.StatusCode, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static OrderDto ToDto(OrderReply r) => new(
        r.Id,
        r.CustomerId,
        r.Status,
        r.OrderDate.ToDateTime(),
        r.ShippingAddress,
        r.HasComment ? r.Comment : null,
        (decimal)r.TotalAmount,
        r.Items.Select(i => new OrderItemDto(
            i.ProductId, i.ProductName, (decimal)i.UnitPrice, (decimal)i.Discount, i.Units)).ToList());
}