using System.Diagnostics;
using Grpc.Core;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public class OrderAggregationService(
    OrdersClient orders,
    CatalogClient catalog,
    ReviewsClient reviews,
    AggregationCache cache,
    ILogger<OrderAggregationService> logger)
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");

    public async Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("GetFullOrder");
        activity?.SetTag("order.id", orderId);

        var cacheKey = AggregationCache.OrderKey(orderId);
        var cached = await cache.GetAsync<FullOrderDto>(cacheKey, ct);
        if (cached is not null)
        {
            activity?.SetTag("cache.hit", true);
            logger.LogInformation("Order {OrderId} served from the aggregation cache", orderId);
            return cached;
        }

        activity?.SetTag("cache.hit", false);

        var warnings = new List<string>();

        var order = await GetOrderAsync(orderId, ct);
        if (order is null)
        {
            logger.LogInformation("Order {OrderId} not found, nothing to aggregate", orderId);
            return null;
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

        var productsTask = SafeAsync(
            token => catalog.GetProductsAsync(productIds, token), "catalog-api", warnings, ct);
        var reviewsTask = SafeAsync(
            token => reviews.GetForProductsAsync(productIds, token), "reviews-api", warnings, ct);

        await Task.WhenAll(productsTask, reviewsTask);

        var catalogResult = await productsTask;
        var reviewsResult = await reviewsTask;

        var products = (catalogResult ?? Array.Empty<ProductDto>())
            .Where(p => productIds.Contains(p.Id))
            .ToList();

        var productReviews = (reviewsResult ?? Array.Empty<ReviewDto>())
            .Where(r => productIds.Contains(r.ProductId))
            .ToList();

        if (catalogResult is not null && products.Count < productIds.Count)
            warnings.Add($"catalog-api returned {products.Count} of {productIds.Count} products");

        activity?.SetTag("warnings.count", warnings.Count);
        logger.LogInformation(
            "Order {OrderId} aggregated: {ProductsCount} products, {ReviewsCount} reviews, {WarningsCount} warnings",
            orderId, products.Count, productReviews.Count, warnings.Count);

        var result = new FullOrderDto(order, products, productReviews, warnings);

        if (warnings.Count == 0)
            await cache.SetAsync(cacheKey, result, ct);
        else
            logger.LogInformation("Order {OrderId} response is partial ({WarningsCount} warnings) and is not cached",
                orderId, warnings.Count);

        return result;
    }

    private async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        try
        {
            return await orders.GetOrderAsync(orderId, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "{Service} is unavailable, gRPC status {GrpcStatus}",
                "orders-api", (ex as RpcException)?.StatusCode);
            throw new UpstreamUnavailableException("orders-api", ex);
        }
    }

    private async Task<T?> SafeAsync<T>(
        Func<CancellationToken, Task<T>> call, string service, List<string> warnings, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await call(ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            var status = (ex as RpcException)?.StatusCode;
            var reason = status == StatusCode.DeadlineExceeded ? "timed out" : "is unavailable";

            logger.LogWarning(ex, "{Service} {Reason} after {Elapsed} ms, gRPC status {GrpcStatus}",
                service, reason, stopwatch.ElapsedMilliseconds, status);

            lock (warnings)
            {
                warnings.Add($"{service} {reason}");
            }

            return default;
        }
    }
}