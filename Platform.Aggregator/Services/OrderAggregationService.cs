using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public class OrderAggregationService(
    OrdersClient orders,
    CatalogClient catalog,
    ReviewsClient reviews,
    ILogger<OrderAggregationService> logger)
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");

    private static readonly TimeSpan DownstreamTimeout = TimeSpan.FromSeconds(5);

    public async Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("GetFullOrder");
        activity?.SetTag("order.id", orderId);

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

        return new FullOrderDto(order, products, productReviews, warnings);
    }

    private async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(DownstreamTimeout);

        try
        {
            return await orders.GetOrderAsync(orderId, deadline.Token);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "{Service} is unavailable", "orders-api");
            throw new UpstreamUnavailableException("orders-api", ex);
        }
    }

    private async Task<T?> SafeAsync<T>(
        Func<CancellationToken, Task<T>> call, string service, List<string> warnings, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(DownstreamTimeout);

        try
        {
            return await call(deadline.Token);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Service} is unavailable after {Elapsed} ms",
                service, stopwatch.ElapsedMilliseconds);

            lock (warnings)
            {
                warnings.Add($"{service} is unavailable");
            }

            return default;
        }
    }
}