using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Platform.Aggregator.Caching;

public class AggregationCache(IDistributedCache cache, ILogger<AggregationCache> logger)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);

    private static readonly TimeSpan RedisTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Backoff = TimeSpan.FromSeconds(15);

    private long _unavailableUntil;

    private bool Available => Environment.TickCount64 >= Volatile.Read(ref _unavailableUntil);

    public static string OrderKey(long orderId) => $"aggregator:order-full:{orderId}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        if (!Available)
            return null;

        try
        {
            var bytes = await cache.GetAsync(key, ct).WaitAsync(RedisTimeout, ct);
            if (bytes is null)
            {
                logger.LogDebug("Aggregation cache miss {Key}", key);
                return null;
            }

            logger.LogDebug("Aggregation cache hit {Key}, size {Bytes} bytes", key, bytes.Length);
            return JsonSerializer.Deserialize<T>(bytes);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Aggregation cache entry {Key} cannot be deserialized and is ignored", key);
            return null;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            MarkUnavailable(ex, "get", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken ct) where T : class
    {
        if (!Available)
            return;

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);

            await cache.SetAsync(
                    key,
                    bytes,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
                    ct)
                .WaitAsync(RedisTimeout, ct);

            logger.LogDebug("Aggregation cache set {Key}, ttl {Ttl}, size {Bytes} bytes", key, Ttl, bytes.Length);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            MarkUnavailable(ex, "set", key);
        }
    }

    private void MarkUnavailable(Exception ex, string operation, string key)
    {
        Volatile.Write(ref _unavailableUntil, Environment.TickCount64 + (long)Backoff.TotalMilliseconds);

        logger.LogWarning(
            ex,
            "Redis {Operation} failed for {Key}; the aggregation cache is skipped for {BackoffSeconds} s",
            operation, key, Backoff.TotalSeconds);
    }
}