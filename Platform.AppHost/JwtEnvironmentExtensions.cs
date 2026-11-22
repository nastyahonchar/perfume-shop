using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

internal static class JwtEnvironmentExtensions
{
    public static IResourceBuilder<T> WithJwtSettings<T>(this IResourceBuilder<T> resource, IConfiguration configuration)
        where T : IResourceWithEnvironment
    {
        var section = configuration.GetSection("Jwt");
        return resource
            .WithEnvironment("Jwt__Issuer", section["Issuer"])
            .WithEnvironment("Jwt__Audience", section["Audience"])
            .WithEnvironment("Jwt__SecretKey", section["SecretKey"]);
    }
}