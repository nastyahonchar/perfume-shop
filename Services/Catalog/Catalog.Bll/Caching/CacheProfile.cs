using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public sealed record CacheProfile(
    TimeSpan? L1Sliding,
    TimeSpan L1Absolute,
    TimeSpan L2Absolute,
    int Size = 1,
    CacheItemPriority Priority = CacheItemPriority.Normal);

public static class CacheProfiles
{
    public static readonly CacheProfile Product = new(
        TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));

    public static readonly CacheProfile Reference = new(
        null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(6), Priority: CacheItemPriority.High);
}