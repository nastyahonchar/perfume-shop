using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Bll.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogCaching(this IServiceCollection services)
    {
        services.AddSingleton<ICatalogCache, TwoLevelCatalogCache>();
        return services;
    }
}