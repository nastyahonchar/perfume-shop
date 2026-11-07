using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public sealed record CacheProfile(
    TimeSpan? Sliding,
    TimeSpan Absolute,
    int Size = 1,
    CacheItemPriority Priority = CacheItemPriority.Normal);

public static class CacheProfiles
{
    public static readonly CacheProfile Product = new(TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

    public static readonly CacheProfile Reference = new(null, TimeSpan.FromHours(6), Priority: CacheItemPriority.High);
}