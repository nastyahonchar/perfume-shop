using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Orders.Bll.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Grpc;

namespace Orders.Api.Grpc;

public class OrdersGrpcService(IOrderService orders) : OrdersService.OrdersServiceBase
{
    public override async Task<OrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Order id must be a positive number"));

        try
        {
            var order = await orders.GetByIdAsync(request.Id, context.CancellationToken);

            var reply = new OrderReply
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                OrderDate = Timestamp.FromDateTime(DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc)),
                ShippingAddress = order.ShippingAddress,
                TotalAmount = (double)order.TotalAmount
            };

            if (order.Comment is not null)
                reply.Comment = order.Comment;

            reply.Items.AddRange(order.Items.Select(i => new OrderItemReply
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = (double)i.UnitPrice,
                Discount = (double)i.Discount,
                Units = i.Units
            }));

            return reply;
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }
}