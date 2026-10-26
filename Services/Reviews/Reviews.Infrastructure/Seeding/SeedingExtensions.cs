using Microsoft.Extensions.DependencyInjection;

namespace Reviews.Infrastructure.Seeding;

public static class SeedingExtensions
{
    public static async Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();

        foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
            await seeder.SeedAsync(ct);
    }
}