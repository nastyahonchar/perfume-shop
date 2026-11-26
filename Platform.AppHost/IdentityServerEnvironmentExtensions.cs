using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

internal static class IdentityServerEnvironmentExtensions
{
    public static IResourceBuilder<T> WithIdentityServerSettings<T>(
        this IResourceBuilder<T> resource, IConfiguration configuration, string? audience = null, bool validateAudience = true)
        where T : IResourceWithEnvironment
    {
        resource
            .WithEnvironment("IdentityServer__Authority", configuration["IdentityServer:Authority"])
            .WithEnvironment("IdentityServer__ValidateAudience", validateAudience.ToString());

        if (audience is not null)
        {
            resource.WithEnvironment("IdentityServer__Audience", audience);
        }

        return resource;
    }

    public static IResourceBuilder<T> WithAggregatorClient<T>(
        this IResourceBuilder<T> resource, IConfiguration configuration)
        where T : IResourceWithEnvironment
    {
        var section = configuration.GetSection("IdentityServer");

        return resource
            .WithEnvironment("IdentityServer__Authority", section["Authority"])
            .WithEnvironment("IdentityServer__ClientId", section["Aggregator:ClientId"])
            .WithEnvironment("IdentityServer__ClientSecret", section["Aggregator:ClientSecret"])
            .WithEnvironment("IdentityServer__Scope", section["Aggregator:Scope"]);
    }
}