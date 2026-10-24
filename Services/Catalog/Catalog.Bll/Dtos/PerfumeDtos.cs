using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public record PerfumeImageDto(int Id, string Url, int SortOrder, bool IsMain);

public record PerfumeDetailDto(string Description, string? Perfumer, short? LaunchYear, byte? Longevity);

public record PerfumeNoteDto(int NoteId, string NoteName, string Family, string Layer);

public record PerfumeListItemDto(
    int Id, string Brand, string Name, string Sku, string Gender, string Concentration,
    string Season, short VolumeMl, decimal Price, bool IsActive);

public record PerfumeDto(
    int Id, int BrandId, string Brand, string Name, string Sku, string Gender, string Concentration,
    string Season, short VolumeMl, decimal Price, bool IsActive,
    PerfumeDetailDto? Detail,
    IReadOnlyList<PerfumeImageDto> Images,
    IReadOnlyList<PerfumeNoteDto> Notes);

public class CreatePerfumeDto
{
    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [Required, StringLength(50)]
    public string Sku { get; set; } = "";

    [Required]
    public string Gender { get; set; } = "";

    [Required]
    public string Concentration { get; set; } = "";

    [Required]
    public string Season { get; set; } = "";

    [Range(1, 1000)]
    public short VolumeMl { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Price { get; set; }

    public CreatePerfumeDetailDto? Detail { get; set; }

    public List<PerfumeNoteInputDto> Notes { get; set; } = new();
}

public class CreatePerfumeDetailDto
{
    [Required, StringLength(2000)]
    public string Description { get; set; } = "";

    [StringLength(100)]
    public string? Perfumer { get; set; }

    public short? LaunchYear { get; set; }

    public byte? Longevity { get; set; }
}

public class PerfumeNoteInputDto
{
    [Range(1, int.MaxValue)]
    public int NoteId { get; set; }

    [Required]
    public string Layer { get; set; } = "";
}

public class UpdatePerfumeDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string Season { get; set; } = "";

    [Range(0.01, 1_000_000)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}

public class ChangePriceDto
{
    [Range(0.01, 1_000_000)]
    public decimal Price { get; set; }
}