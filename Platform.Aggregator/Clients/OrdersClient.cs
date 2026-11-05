using System.Diagnostics;
using System.Net;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class OrdersClient(HttpClient http, ILogger<OrdersClient> logger)
{
    private const string ServiceName = "orders-api";

    public async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        logger.LogInformation("Calling {Service} for order {OrderId}", ServiceName, orderId);
        var stopwatch = Stopwatch.StartNew();

        using var response = await http.GetAsync($"/api/orders/{orderId}", ct);

        logger.LogInformation("{Service} responded {StatusCode} in {Elapsed} ms",
            ServiceName, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }
}