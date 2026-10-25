namespace Catalog.Domain.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public short? FoundedYear { get; set; }

    public List<Perfume> Perfumes { get; } = new();
}