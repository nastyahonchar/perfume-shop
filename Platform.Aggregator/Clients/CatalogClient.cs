using System.Diagnostics;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class CatalogClient(HttpClient http, ILogger<CatalogClient> logger)
{
    private const string ServiceName = "catalog-api";

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        IReadOnlyCollection<long> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0)
            return [];

        logger.LogInformation("Calling {Service} for {ProductsCount} products", ServiceName, productIds.Count);
        var stopwatch = Stopwatch.StartNew();

        var query = string.Join("&", productIds.Select(id => $"ids={id}"));
        var products = await http.GetFromJsonAsync<List<ProductDto>>($"/api/perfumes/by-ids?{query}", ct) ?? [];

        logger.LogInformation("{Service} responded in {Elapsed} ms with {ProductsCount} products",
            ServiceName, stopwatch.ElapsedMilliseconds, products.Count);

        return products;
    }
}