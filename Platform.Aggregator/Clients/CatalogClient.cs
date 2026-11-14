using Catalog.Grpc;
using Grpc.Core;
using Platform.Aggregator.Models;
using Platform.Aggregator.Resilience;
using System.Diagnostics;

namespace Platform.Aggregator.Clients;

public class CatalogClient(CatalogService.CatalogServiceClient grpc, ILogger<CatalogClient> logger)
{
    private const string ServiceName = "catalog-api";

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        IReadOnlyCollection<long> productIds, CancellationToken ct)
    {
        var ids = productIds.Where(id => id is > 0 and <= int.MaxValue).Select(id => (int)id).ToList();
        if (ids.Count == 0)
            return [];

        logger.LogInformation("Calling {Service} gRPC GetProductsByIds for {ProductsCount} products",
            ServiceName, ids.Count);
        var stopwatch = Stopwatch.StartNew();

        var request = new GetProductsByIdsRequest();
        request.Ids.AddRange(ids);

        try
        {
            var reply = await grpc.GetProductsByIdsAsync(
                request, deadline: ResilienceProfiles.Catalog.CallDeadline(), cancellationToken: ct);

            logger.LogInformation("{Service} responded {GrpcStatus} in {Elapsed} ms with {ProductsCount} products",
                ServiceName, StatusCode.OK, stopwatch.ElapsedMilliseconds, reply.Products.Count);

            return reply.Products.Select(ToDto).ToList();
        }
        catch (RpcException ex)
        {
            logger.LogWarning("{Service} failed with {GrpcStatus} in {Elapsed} ms",
                ServiceName, ex.StatusCode, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static ProductDto ToDto(ProductReply p) => new(
        p.Id, p.Brand, p.Name, p.Sku, p.Gender, p.Concentration, p.Season,
        (short)p.VolumeMl, (decimal)p.Price, p.IsActive);
}