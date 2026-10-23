namespace Orders.Domain.Models;
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string Season { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}