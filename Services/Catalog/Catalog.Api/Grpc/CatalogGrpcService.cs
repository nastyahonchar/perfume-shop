using Catalog.Api.Metrics;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using Grpc.Core;

namespace Catalog.Api.Grpc;

public class CatalogGrpcService(IPerfumeService perfumes, CatalogMetrics metrics) : CatalogService.CatalogServiceBase
{
    public override async Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be a positive number"));

        try
        {
            var perfume = await metrics.MeasureAsync("get", () => perfumes.GetByIdAsync(request.Id, context.CancellationToken));

            return new ProductReply
            {
                Id = perfume.Id,
                Brand = perfume.Brand,
                Name = perfume.Name,
                Sku = perfume.Sku,
                Gender = perfume.Gender,
                Concentration = perfume.Concentration,
                Season = perfume.Season,
                VolumeMl = perfume.VolumeMl,
                Price = (double)perfume.Price,
                IsActive = perfume.IsActive
            };
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ProductsReply> GetProductsByIds(GetProductsByIdsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await metrics.MeasureAsync("get_many", () => perfumes.GetByIdsAsync(request.Ids.ToList(), context.CancellationToken));

            var reply = new ProductsReply();
            reply.Products.AddRange(items.Select(ToReply));
            return reply;
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private static ProductReply ToReply(PerfumeListItemDto p) => new()
    {
        Id = p.Id,
        Brand = p.Brand,
        Name = p.Name,
        Sku = p.Sku,
        Gender = p.Gender,
        Concentration = p.Concentration,
        Season = p.Season,
        VolumeMl = p.VolumeMl,
        Price = (double)p.Price,
        IsActive = p.IsActive
    };
}