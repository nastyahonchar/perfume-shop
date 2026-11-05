using System.Diagnostics;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class ReviewsClient(HttpClient http, ILogger<ReviewsClient> logger)
{
    private const string ServiceName = "reviews-api";
    private const int ReviewsPerProduct = 10;

    public async Task<IReadOnlyList<ReviewDto>> GetForProductsAsync(
        IReadOnlyCollection<long> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0)
            return [];

        logger.LogInformation("Calling {Service} for reviews of {ProductsCount} products",
            ServiceName, productIds.Count);
        var stopwatch = Stopwatch.StartNew();

        var pages = await Task.WhenAll(productIds.Select(id => GetPageAsync(id, ct)));
        var reviews = pages.SelectMany(page => page.Items).ToList();

        logger.LogInformation("{Service} responded in {Elapsed} ms with {ReviewsCount} reviews",
            ServiceName, stopwatch.ElapsedMilliseconds, reviews.Count);

        return reviews;
    }

    private async Task<PagedResultDto<ReviewDto>> GetPageAsync(long productId, CancellationToken ct)
        => await http.GetFromJsonAsync<PagedResultDto<ReviewDto>>(
               $"/api/reviews?productId={productId}&page=1&pageSize={ReviewsPerProduct}", ct)
           ?? new PagedResultDto<ReviewDto>([], 0, 1, ReviewsPerProduct, 0, false, false);
}