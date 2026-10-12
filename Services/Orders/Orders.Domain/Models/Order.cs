namespace Orders.Domain.Models;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string ShippingAddress { get; set; } = "";
    public string? Comment { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}