namespace Catalog.Bll.Queries;

public class PerfumeQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }
    public int? BrandId { get; set; }
    public string? Season { get; set; }
    public string? Gender { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public List<string>? Notes { get; set; }

    public string SortBy { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
}