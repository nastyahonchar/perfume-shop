namespace Orders.Domain.Models;

public class OrderSummary
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}