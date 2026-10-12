using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Domain.Models;

namespace Orders.Bll.Mapping;

public class OrdersProfile : Profile
{
    public OrdersProfile()
    {
        CreateMap<Customer, CustomerDto>();
        CreateMap<Product, ProductDto>();
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<OrderSummary, OrderSummaryDto>();
        CreateMap<Order, OrderDto>()
            .ForCtorParam(nameof(OrderDto.TotalAmount),
                o => o.MapFrom(src => src.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Units)));
    }
}