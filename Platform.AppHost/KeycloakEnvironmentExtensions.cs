using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

internal static class KeycloakEnvironmentExtensions
{
    public static IResourceBuilder<T> WithKeycloakSettings<T>(
        this IResourceBuilder<T> resource, IConfiguration configuration, string? audience = null, bool validateAudience = true)
        where T : IResourceWithEnvironment
    {
        resource
            .WithEnvironment("Keycloak__Authority", configuration["Keycloak:Authority"])
            .WithEnvironment("Keycloak__ValidateAudience", validateAudience.ToString());

        if (audience is not null)
        {
            resource.WithEnvironment("Keycloak__Audience", audience);
        }

        return resource;
    }
}