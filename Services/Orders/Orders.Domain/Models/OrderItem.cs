namespace Orders.Domain.Models;

public class OrderItem
{
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }   
    public int Units { get; set; }
}