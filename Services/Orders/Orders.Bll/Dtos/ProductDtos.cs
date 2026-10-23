using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.Dtos;

public record ProductDto(
    long Id, string Name, string BrandName, string Season, decimal Price, int Stock, bool IsActive);

public class SetStockDto
{
    [Range(0, 1_000_000)]
    public int Stock { get; set; }
}