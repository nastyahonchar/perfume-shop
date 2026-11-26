using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Platform.IdentityServer;

public static class ConfigSeeder
{
    public static async Task SeedAsync(ConfigurationDbContext context)
    {
        foreach (var resource in Config.IdentityResources)
        {
            if (!await context.IdentityResources.AnyAsync(r => r.Name == resource.Name))
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
        }

        foreach (var scope in Config.ApiScopes)
        {
            if (!await context.ApiScopes.AnyAsync(s => s.Name == scope.Name))
            {
                context.ApiScopes.Add(scope.ToEntity());
            }
        }

        foreach (var resource in Config.ApiResources)
        {
            if (!await context.ApiResources.AnyAsync(r => r.Name == resource.Name))
            {
                context.ApiResources.Add(resource.ToEntity());
            }
        }

        foreach (var client in Config.Clients)
        {
            if (!await context.Clients.AnyAsync(c => c.ClientId == client.ClientId))
            {
                context.Clients.Add(client.ToEntity());
            }
        }

        await context.SaveChangesAsync();
    }
}