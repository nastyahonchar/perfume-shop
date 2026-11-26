using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Platform.ServiceDefaults;

public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

// Політика з іменем scope (наприклад "orders.read") пропускає токен IdentityServer лише тоді, коли в ньому є
// такий scope: це дає fine-grained доступ, окремо на читання і на запис. Токени Auth Service (користувачі платформи)
// scope не містять, їх довіру забезпечує підпис ключем Auth Service (claim iss збігається з Jwt:Issuer).
public sealed class ScopeAuthorizationHandler(IConfiguration configuration) : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var user = context.User;

        var hasScope = user.FindAll("scope")
            .Any(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(requirement.Scope));

        var localIssuer = configuration["Jwt:Issuer"];
        var isLocalUser = !string.IsNullOrEmpty(localIssuer) && user.HasClaim("iss", localIssuer);

        if (hasScope || isLocalUser)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class ScopeAuthorizationExtensions
{
    public static IServiceCollection AddScopePolicies(this IServiceCollection services, params string[] scopes)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeAuthorizationHandler>());

        services.AddAuthorization(options =>
        {
            foreach (var scope in scopes)
            {
                options.AddPolicy(scope, policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ScopeRequirement(scope)));
            }
        });

        return services;
    }
}