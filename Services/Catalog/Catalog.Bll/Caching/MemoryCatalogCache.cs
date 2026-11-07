using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Caching;

public sealed class MemoryCatalogCache(IMemoryCache memory, ILogger<MemoryCatalogCache> logger) : ICatalogCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        if (memory.TryGetValue(key, out T? value))
        {
            logger.LogDebug("Cache hit {Key}, level {Level}", key, "L1");
            return Task.FromResult(value);
        }

        logger.LogDebug("Cache miss {Key}, level {Level}", key, "L1");
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, CacheProfile profile, CancellationToken ct) where T : class
    {
        memory.Set(key, value, new MemoryCacheEntryOptions
        {
            Size = profile.Size,
            SlidingExpiration = profile.Sliding,
            AbsoluteExpirationRelativeToNow = profile.Absolute,
            Priority = profile.Priority
        });

        logger.LogDebug(
            "Cache set {Key}, level {Level}, size {Size}, sliding {Sliding}, absolute {Absolute}",
            key, "L1", profile.Size, profile.Sliding, profile.Absolute);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(IReadOnlyCollection<string> keys, string reason, CancellationToken ct)
    {
        foreach (var key in keys)
            memory.Remove(key);

        logger.LogInformation("Cache invalidated {Keys}, reason {Reason}", keys, reason);
        return Task.CompletedTask;
    }
}