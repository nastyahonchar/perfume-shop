using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Bll.Interfaces;
using Orders.Dal.Interfaces;
using Orders.Domain.Constants;
using Orders.Domain.Exceptions;

namespace Orders.Bll.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct)
    {
        var products = await _uow.Products.GetAllAsync(ct);
        return products.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct)
                      ?? throw new NotFoundException($"Product {id} not found");
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetBySeasonAsync(string season, CancellationToken ct)
    {
        if (!Seasons.All.Contains(season))
            throw new ValidationException($"Unknown season '{season}'. Allowed: {string.Join(", ", Seasons.All)}");

        var products = await _uow.Products.GetBySeasonAsync(season, ct);
        return products.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<ProductDto> SetStockAsync(long id, int stock, CancellationToken ct)
    {
        if (stock < 0)
            throw new ValidationException("Stock cannot be negative");

        var product = await _uow.Products.GetByIdAsync(id, ct)
                      ?? throw new NotFoundException($"Product {id} not found");

        product.Stock = stock;
        await _uow.Products.UpdateAsync(product, ct);
        return _mapper.Map<ProductDto>(product);
    }
}