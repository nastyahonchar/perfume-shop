using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public BrandService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct)
    {
        var brands = await _uow.Brands.GetAllAsync(ct);
        return brands.OrderBy(b => b.Name).Select(b => _mapper.Map<BrandDto>(b)).ToList();
    }

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
        return _mapper.Map<BrandDto>(brand);
    }
}