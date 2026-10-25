using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Queries;
using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Constants;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public class PerfumeService : IPerfumeService
{
    private const string ChangedBy = "catalog-api";
    private const int MaxIds = 100;

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public PerfumeService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<PerfumeListItemDto>> GetListAsync(PerfumeQuery query, CancellationToken ct)
    {
        query.Page = Math.Max(query.Page, 1);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
        query.Season = Canonical(query.Season, Seasons.All, "season");
        query.Gender = Canonical(query.Gender, Genders.All, "gender");

        var spec = new PerfumesFilterSpec(query);
        var items = await _uow.Perfumes.ListAsync(spec, ct);
        var total = await _uow.Perfumes.CountAsync(spec, ct);

        return new PagedResultDto<PerfumeListItemDto>(
            items.Select(p => _mapper.Map<PerfumeListItemDto>(p)).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<PerfumeDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var perfume = await _uow.Perfumes.GetWithDetailsAsync(id, ct)
                      ?? throw new EntityNotFoundException($"Perfume {id} not found");
        return _mapper.Map<PerfumeDto>(perfume);
    }

    public async Task<IReadOnlyList<PerfumeListItemDto>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0)
            throw new ValidationException("At least one id is required");
        if (distinct.Count > MaxIds)
            throw new ValidationException($"At most {MaxIds} ids can be requested at once");

        var items = await _uow.Perfumes.ListAsync(new PerfumesByIdsSpec(distinct), ct);
        return items.Select(p => _mapper.Map<PerfumeListItemDto>(p)).ToList();
    }

    public async Task<IReadOnlyList<PerfumeListItemDto>> GetByNoteAsync(string noteName, string? layer, CancellationToken ct)
    {
        var canonicalLayer = Canonical(layer, NoteLayers.All, "layer");
        var name = noteName.Trim();

        if (!await _uow.Notes.ExistsByNameAsync(name, ct))
            throw new EntityNotFoundException($"Note '{name}' not found");

        var items = await _uow.PerfumeNotes.GetPerfumesByNoteAsync(name, canonicalLayer, ct);
        return items.Select(p => _mapper.Map<PerfumeListItemDto>(p)).ToList();
    }

    public async Task<PerfumeDto> CreateAsync(CreatePerfumeDto input, CancellationToken ct)
    {
        var brand = await _uow.Brands.GetByIdAsync(input.BrandId, ct)
                    ?? throw new EntityNotFoundException($"Brand {input.BrandId} not found");

        var sku = input.Sku.Trim();
        if (await _uow.Perfumes.ExistsBySkuAsync(sku, ct))
            throw new ConflictException($"Perfume with SKU '{sku}' already exists");

        var name = input.Name.Trim();
        if (await _uow.Perfumes.ExistsByBrandNameVolumeAsync(brand.Id, name, input.VolumeMl, ct))
            throw new ConflictException($"Perfume '{name}' ({input.VolumeMl} ml) of brand '{brand.Name}' already exists");

        var perfume = new Perfume(
            brand.Id, name, sku, input.Gender, input.Concentration, input.Season,
            input.VolumeMl, input.Price, ChangedBy);

        if (input.Detail is not null)
            perfume.SetDetail(input.Detail.Description, input.Detail.Perfumer, input.Detail.LaunchYear, input.Detail.Longevity);

        foreach (var note in input.Notes)
        {
            if (await _uow.Notes.GetByIdAsync(note.NoteId, ct) is null)
                throw new EntityNotFoundException($"Note {note.NoteId} not found");

            perfume.AddNote(note.NoteId, note.Layer);
        }

        await _uow.Perfumes.AddAsync(perfume, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(perfume.Id, ct);
    }

    public async Task<PerfumeDto> UpdateAsync(int id, UpdatePerfumeDto input, CancellationToken ct)
    {
        var perfume = await _uow.Perfumes.GetWithDetailsAsync(id, ct)
                      ?? throw new EntityNotFoundException($"Perfume {id} not found");

        var name = input.Name.Trim();
        if (!string.Equals(perfume.Name, name, StringComparison.Ordinal))
        {
            if (await _uow.Perfumes.ExistsByBrandNameVolumeAsync(perfume.BrandId, name, perfume.VolumeMl, ct))
                throw new ConflictException($"Perfume '{name}' ({perfume.VolumeMl} ml) of this brand already exists");
            perfume.Rename(name, ChangedBy);
        }

        if (perfume.Price != input.Price)
            perfume.ChangePrice(input.Price, ChangedBy);

        var season = Canonical(input.Season, Seasons.All, "season")!;
        if (perfume.Season != season)
            perfume.ChangeSeason(season, ChangedBy);

        if (perfume.IsActive != input.IsActive)
        {
            if (input.IsActive) perfume.Activate(ChangedBy);
            else perfume.Deactivate(ChangedBy);
        }

        await _uow.SaveChangesAsync(ct);
        return _mapper.Map<PerfumeDto>(perfume);
    }

    public async Task<PerfumeDto> ChangePriceAsync(int id, ChangePriceDto input, CancellationToken ct)
    {
        var perfume = await _uow.Perfumes.GetWithDetailsAsync(id, ct)
                      ?? throw new EntityNotFoundException($"Perfume {id} not found");

        perfume.ChangePrice(input.Price, ChangedBy);

        await _uow.SaveChangesAsync(ct);
        return _mapper.Map<PerfumeDto>(perfume);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var perfume = await _uow.Perfumes.GetWithDetailsAsync(id, ct)
                      ?? throw new EntityNotFoundException($"Perfume {id} not found");

        perfume.MarkDeleted(ChangedBy);
        await _uow.SaveChangesAsync(ct);
    }

    private static string? Canonical(string? value, string[] allowed, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? throw new ValidationException(
                   $"Unknown {field} '{value}'. Allowed: {string.Join(", ", allowed)}");
    }
}