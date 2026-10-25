using Catalog.Domain.Constants;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Perfume
{
    public int Id { get; private set; }
    public int BrandId { get; private set; }
    public Brand Brand { get; private set; } = null!;
    public string Name { get; private set; } = "";
    public string Sku { get; private set; } = "";
    public string Gender { get; private set; } = "";
    public string Concentration { get; private set; } = "";
    public string Season { get; private set; } = Seasons.AllYear;
    public short VolumeMl { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public PerfumeDetail? Detail { get; private set; }
    public List<PerfumeImage> Images { get; } = new();
    public List<PerfumeNote> PerfumeNotes { get; } = new();

    private Perfume() { }

    public Perfume(
        int brandId, string name, string sku, string gender, string concentration,
        string season, short volumeMl, decimal price, string? createdBy = null)
    {
        if (brandId <= 0)
            throw new DomainException("Brand is required");
        if (volumeMl <= 0)
            throw new DomainException("Volume must be positive");
        if (price <= 0)
            throw new DomainException("Price must be positive");

        BrandId = brandId;
        Name = NormalizeName(name);
        Sku = NormalizeSku(sku);
        Gender = EnsureOneOf(gender, Genders.All, "gender");
        Concentration = EnsureOneOf(concentration, Concentrations.All, "concentration");
        Season = EnsureOneOf(season, Seasons.All, "season");
        VolumeMl = volumeMl;
        Price = price;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void ChangePrice(decimal newPrice, string? updatedBy = null)
    {
        if (newPrice <= 0)
            throw new DomainException("Price must be positive");

        Price = newPrice;
        Touch(updatedBy);
    }

    public void Rename(string newName, string? updatedBy = null)
    {
        Name = NormalizeName(newName);
        Touch(updatedBy);
    }

    public void ChangeSeason(string season, string? updatedBy = null)
    {
        Season = EnsureOneOf(season, Seasons.All, "season");
        Touch(updatedBy);
    }

    public void Activate(string? updatedBy = null)
    {
        IsActive = true;
        Touch(updatedBy);
    }

    public void Deactivate(string? updatedBy = null)
    {
        IsActive = false;
        Touch(updatedBy);
    }

    public void MarkDeleted(string? updatedBy = null)
    {
        IsDeleted = true;
        IsActive = false;
        Touch(updatedBy);
    }

    public void SetDetail(string description, string? perfumer, short? launchYear, byte? longevity)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required");
        if (description.Trim().Length > 2000)
            throw new DomainException("Description is too long (max 2000 characters)");
        if (launchYear is not null && (launchYear < 1800 || launchYear > 2100))
            throw new DomainException("Launch year must be between 1800 and 2100");
        if (longevity is not null && (longevity < 1 || longevity > 5))
            throw new DomainException("Longevity must be between 1 and 5");

        Detail = new PerfumeDetail
        {
            Description = description.Trim(),
            Perfumer = string.IsNullOrWhiteSpace(perfumer) ? null : perfumer.Trim(),
            LaunchYear = launchYear,
            Longevity = longevity
        };
        Touch(null);
    }

    public void AddImage(string url, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Image url is required");

        if (isMain)
            foreach (var existing in Images)
                existing.IsMain = false;

        Images.Add(new PerfumeImage { Url = url.Trim(), SortOrder = Images.Count, IsMain = isMain });
    }

    public void AddNote(int noteId, string layer)
    {
        if (noteId <= 0)
            throw new DomainException("Note is required");

        var normalizedLayer = EnsureOneOf(layer, NoteLayers.All, "layer");
        if (PerfumeNotes.Any(pn => pn.NoteId == noteId))
            throw new DomainException($"Note {noteId} is already added to this perfume");

        PerfumeNotes.Add(new PerfumeNote { NoteId = noteId, Layer = normalizedLayer, AddedAt = DateTime.UtcNow });
    }

    private void Touch(string? updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");

        var trimmed = name.Trim();
        if (trimmed.Length > 200)
            throw new DomainException("Name is too long (max 200 characters)");
        return trimmed;
    }

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required");

        var trimmed = sku.Trim();
        if (trimmed.Length > 50)
            throw new DomainException("SKU is too long (max 50 characters)");
        return trimmed;
    }

    private static string EnsureOneOf(string value, string[] allowed, string fieldName)
    {
        var match = allowed.FirstOrDefault(a => string.Equals(a, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw new DomainException($"Unknown {fieldName} '{value}'. Allowed: {string.Join(", ", allowed)}");
    }
}