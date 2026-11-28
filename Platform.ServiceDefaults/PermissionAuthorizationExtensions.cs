using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Platform.ServiceDefaults;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

// Токен Keycloak (RPT, отриманий через UMA-grant) містить claim "authorization" з JSON
// { "permissions": [ { "rsname": "orders-api", "scopes": ["orders:read", ...] } ] }.
// Handler парсить цей JSON і шукає серед scopes потрібний permission. Рішення приймає сам сервіс локально,
// без звернення до Keycloak на кожен запит. Якщо permission немає, requirement не виконується і клієнт отримує 403.
// Токени Auth Service та IdentityServer не містять цього claim: для них діють scope-політики (ScopeRequirement),
// тому тут вони пропускаються за issuer.
public sealed class PermissionAuthorizationHandler(IConfiguration configuration)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var keycloakIssuer = configuration["Keycloak:Authority"]?.TrimEnd('/');
        var isKeycloakToken = !string.IsNullOrEmpty(keycloakIssuer) && context.User.HasClaim("iss", keycloakIssuer);

        if (!isKeycloakToken)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var authorizationClaim = context.User.FindFirst("authorization")?.Value;
        if (authorizationClaim is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            using var document = JsonDocument.Parse(authorizationClaim);

            if (document.RootElement.TryGetProperty("permissions", out var permissions)
                && permissions.ValueKind == JsonValueKind.Array)
            {
                foreach (var permission in permissions.EnumerateArray())
                {
                    if (!permission.TryGetProperty("scopes", out var scopes) || scopes.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    if (scopes.EnumerateArray().Any(s => s.GetString() == requirement.Permission))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return Task.CompletedTask;
    }
}

// [RequirePermission("orders:create")] це політика авторизації з іменем permission. Політики для всіх permissions
// сервісу реєструються одним викликом AddPermissionPolicies у Program.cs.
public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute(permission);

public static class PermissionAuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services, params string[] permissions)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());

        services.AddAuthorization(options =>
        {
            foreach (var permission in permissions)
            {
                options.AddPolicy(permission, policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission)));
            }
        });

        return services;
    }
}