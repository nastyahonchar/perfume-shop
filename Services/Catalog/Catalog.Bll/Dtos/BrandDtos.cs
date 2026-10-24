using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public record BrandDto(int Id, string Name, string? Country, short? FoundedYear);

public class SaveBrandDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(60)]
    public string? Country { get; set; }

    public short? FoundedYear { get; set; }
}