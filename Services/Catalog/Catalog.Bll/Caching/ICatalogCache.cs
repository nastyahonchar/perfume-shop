namespace Catalog.Bll.Caching;

public interface ICatalogCache
{
    Task<T?> GetAsync<T>(string key, CacheProfile profile, CancellationToken ct) where T : class;

    Task SetAsync<T>(string key, T value, CacheProfile profile, CancellationToken ct) where T : class;

    Task RemoveAsync(IReadOnlyCollection<string> keys, string reason, CancellationToken ct);
}