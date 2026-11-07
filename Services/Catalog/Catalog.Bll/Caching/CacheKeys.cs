namespace Catalog.Bll.Caching;

public static class CacheKeys
{
    public static string Product(int id) => $"catalog:product:{id}";

    public static string ProductItem(int id) => $"catalog:product-item:{id}";

    public const string Brands = "catalog:brands:all";

    public static string Notes(string? family) =>
        $"catalog:notes:{(string.IsNullOrWhiteSpace(family) ? "all" : family.Trim().ToLowerInvariant())}";
}