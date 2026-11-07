namespace Catalog.Bll.Caching;

public static class CatalogCacheExtensions
{
    public static async Task<T?> GetOrCreateAsync<T>(
        this ICatalogCache cache,
        string key,
        CacheProfile profile,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct) where T : class
    {
        var cached = await cache.GetAsync<T>(key, ct);
        if (cached is not null)
            return cached;

        var value = await factory(ct);
        if (value is not null)
            await cache.SetAsync(key, value, profile, ct);

        return value;
    }
}