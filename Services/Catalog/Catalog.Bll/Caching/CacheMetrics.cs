using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public sealed class CacheMetrics
{
    public const string MeterName = "Catalog.Cache";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;
    private readonly Counter<long> _evictions;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _hits = meter.CreateCounter<long>("cache.hits", description: "Cache hits by level (L1 memory, L2 Redis)");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Cache misses by level (L1 memory, L2 Redis)");
        _invalidations = meter.CreateCounter<long>("cache.invalidations", description: "Cache keys invalidated after writes");
        _evictions = meter.CreateCounter<long>("cache.evictions", description: "Entries evicted from the in-memory cache");
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level));

    public void Miss(string level) => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level));

    public void Invalidated(int keysCount) => _invalidations.Add(keysCount);

    public void Evicted(EvictionReason reason)
        => _evictions.Add(1, new KeyValuePair<string, object?>("cache.reason", reason.ToString()));
}