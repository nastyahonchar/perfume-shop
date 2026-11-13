using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Caching;

public sealed class TwoLevelCatalogCache(
    IMemoryCache memory,
    IDistributedCache distributed,
    CacheMetrics metrics,
    ILogger<TwoLevelCatalogCache> logger) : ICatalogCache
{
    private static readonly TimeSpan L2Timeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan L2Backoff = TimeSpan.FromSeconds(15);

    private long _l2UnavailableUntil;

    private bool L2Available => Environment.TickCount64 >= Volatile.Read(ref _l2UnavailableUntil);

    public async Task<T?> GetAsync<T>(string key, CacheProfile profile, CancellationToken ct) where T : class
    {
        if (memory.TryGetValue(key, out T? local))
        {
            metrics.Hit("L1");
            logger.LogDebug("Cache hit {Key}, level {Level}", key, "L1");
            return local;
        }

        metrics.Miss("L1");
        logger.LogDebug("Cache miss {Key}, level {Level}", key, "L1");

        if (!L2Available)
            return null;

        try
        {
            var bytes = await distributed.GetAsync(key, ct).WaitAsync(L2Timeout, ct);
            if (bytes is null)
            {
                metrics.Miss("L2");
                logger.LogDebug("Cache miss {Key}, level {Level}", key, "L2");
                return null;
            }

            var value = JsonSerializer.Deserialize<T>(bytes);
            if (value is null)
                return null;

            metrics.Hit("L2");
            logger.LogDebug("Cache hit {Key}, level {Level}, size {Bytes} bytes", key, "L2", bytes.Length);

            SetLocal(key, value, profile);
            return value;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cache entry {Key} in L2 cannot be deserialized and is ignored", key);
            return null;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            MarkL2Unavailable(ex, "get", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheProfile profile, CancellationToken ct) where T : class
    {
        SetLocal(key, value, profile);

        if (!L2Available)
            return;

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);

            await distributed.SetAsync(
                    key,
                    bytes,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = profile.L2Absolute },
                    ct)
                .WaitAsync(L2Timeout, ct);

            logger.LogDebug(
                "Cache set {Key}, level {Level}, absolute {Absolute}, size {Bytes} bytes",
                key, "L2", profile.L2Absolute, bytes.Length);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            MarkL2Unavailable(ex, "set", key);
        }
    }

    public async Task RemoveAsync(IReadOnlyCollection<string> keys, string reason, CancellationToken ct)
    {
        foreach (var key in keys)
            memory.Remove(key);

        try
        {
            await Task.WhenAll(keys.Select(key => distributed.RemoveAsync(key, ct))).WaitAsync(L2Timeout, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            MarkL2Unavailable(ex, "remove", string.Join(", ", keys));
        }

        metrics.Invalidated(keys.Count);
        logger.LogInformation("Cache invalidated {Keys}, reason {Reason}", keys, reason);
    }

    private void SetLocal<T>(string key, T value, CacheProfile profile) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            Size = profile.Size,
            SlidingExpiration = profile.L1Sliding,
            AbsoluteExpirationRelativeToNow = profile.L1Absolute,
            Priority = profile.Priority
        };

        options.RegisterPostEvictionCallback(
            static (_, _, reason, state) =>
            {
                if (reason is EvictionReason.Capacity or EvictionReason.Expired or EvictionReason.TokenExpired)
                    ((CacheMetrics)state!).Evicted(reason);
            },
            metrics);

        memory.Set(key, value, options);

        logger.LogDebug(
            "Cache set {Key}, level {Level}, size {Size}, sliding {Sliding}, absolute {Absolute}",
            key, "L1", profile.Size, profile.L1Sliding, profile.L1Absolute);
    }

    private void MarkL2Unavailable(Exception ex, string operation, string key)
    {
        Volatile.Write(ref _l2UnavailableUntil, Environment.TickCount64 + (long)L2Backoff.TotalMilliseconds);

        logger.LogWarning(
            ex,
            "Redis (L2) {Operation} failed for {Key}; L2 is skipped for {BackoffSeconds} s, service continues with L1 and database",
            operation, key, L2Backoff.TotalSeconds);
    }
}