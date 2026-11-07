using AutoMapper;
using Catalog.Bll.Caching;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Catalog.Bll.Specifications;

namespace Catalog.Bll.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICatalogCache _cache;

    public BrandService(IUnitOfWork uow, IMapper mapper, ICatalogCache cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct)
        => await _cache.GetOrCreateAsync<IReadOnlyList<BrandDto>>(CacheKeys.Brands, CacheProfiles.Reference, async token =>
        {
            var brands = await _uow.Brands.GetAllAsync(token);
            return brands.OrderBy(b => b.Name).Select(b => _mapper.Map<BrandDto>(b)).ToList();
        }, ct) ?? [];

    public async Task<BrandDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var brand = await _uow.Brands.GetByIdAsync(id, ct)
                    ?? throw new EntityNotFoundException($"Brand {id} not found");
        return _mapper.Map<BrandDto>(brand);
    }

    public async Task<BrandDto> CreateAsync(SaveBrandDto input, CancellationToken ct)
    {
        var name = input.Name.Trim();
        if (await _uow.Brands.CountAsync(new BrandByNameSpec(name), ct) > 0)
            throw new ConflictException($"Brand '{name}' already exists");

        var brand = new Brand
        {
            Name = name,
            Country = string.IsNullOrWhiteSpace(input.Country) ? null : input.Country.Trim(),
            FoundedYear = input.FoundedYear
        };

        await _uow.Brands.AddAsync(brand, ct);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync([CacheKeys.Brands], "BrandCreated", ct);
        return _mapper.Map<BrandDto>(brand);
    }

    public async Task<BrandDto> UpdateAsync(int id, SaveBrandDto input, CancellationToken ct)
    {
        var brand = await _uow.Brands.GetByIdAsync(id, ct)
                    ?? throw new EntityNotFoundException($"Brand {id} not found");

        var name = input.Name.Trim();
        if (!string.Equals(brand.Name, name, StringComparison.Ordinal)
            && await _uow.Brands.CountAsync(new BrandByNameSpec(name), ct) > 0)
            throw new ConflictException($"Brand '{name}' already exists");

        brand.Name = name;
        brand.Country = string.IsNullOrWhiteSpace(input.Country) ? null : input.Country.Trim();
        brand.FoundedYear = input.FoundedYear;

        await _uow.SaveChangesAsync(ct);
        var perfumes = await _uow.Perfumes.ListAsync(new PerfumesByBrandSpec(id), ct);

        var keys = new List<string> { CacheKeys.Brands };
        foreach (var perfume in perfumes)
        {
            keys.Add(CacheKeys.Product(perfume.Id));
            keys.Add(CacheKeys.ProductItem(perfume.Id));
        }

        await _cache.RemoveAsync(keys, "BrandUpdated", ct);
        return _mapper.Map<BrandDto>(brand);
    }
}