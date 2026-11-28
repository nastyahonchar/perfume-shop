using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Platform.ServiceDefaults;

public static class JwtAuthenticationExtensions
{
    public const string PlatformScheme = "Platform";
    public const string IdentityServerScheme = "IdentityServer";
    public const string KeycloakScheme = "Keycloak";

    // Спільна валідація access token. Схема "Platform" дивиться на issuer токена й вибирає перевірку:
    // токени Auth Service перевіряються за підписом HMAC SHA-256 (Jwt:Issuer, Jwt:Audience, Jwt:SecretKey),
    // токени Duende IdentityServer та Keycloak через Authority (IdentityServer:Authority, Keycloak:Authority):
    // ключі підпису завантажуються автоматично з discovery document, а Audience це ім'я ApiResource
    // (IdentityServer) або resource server (Keycloak) сервісу.
    // Сервіс перевіряє токен локально, не звертаючись до Auth Service, IdentityServer чи Keycloak при кожному запиті.
    public static IServiceCollection AddPlatformJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = PlatformScheme;
            options.DefaultAuthenticateScheme = PlatformScheme;
            options.DefaultChallengeScheme = PlatformScheme;
        })
            .AddPolicyScheme(PlatformScheme, "Auth Service, IdentityServer or Keycloak",
                options => options.ForwardDefaultSelector = SelectScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { })
            .AddJwtBearer(IdentityServerScheme, _ => { })
            .AddJwtBearer(KeycloakScheme, _ => { });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var section = configuration.GetSection("Jwt");
                var secretKey = section["SecretKey"]
                    ?? throw new InvalidOperationException("Configuration value 'Jwt:SecretKey' is missing");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = section["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = section["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddOptions<JwtBearerOptions>(IdentityServerScheme)
            .Configure<IConfiguration>((options, configuration) =>
                ConfigureAuthority(options, configuration.GetSection("IdentityServer")));

        services.AddOptions<JwtBearerOptions>(KeycloakScheme)
            .Configure<IConfiguration>((options, configuration) =>
                ConfigureAuthority(options, configuration.GetSection("Keycloak")));

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureAuthority(JwtBearerOptions options, IConfigurationSection section)
    {
        options.Authority = section["Authority"];
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = section.GetValue("ValidateAudience", true),
            ValidAudience = section["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    private static string SelectScheme(HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var header = context.Request.Headers.Authorization.ToString();

        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header["Bearer ".Length..].Trim();
            var handler = new JsonWebTokenHandler();

            if (handler.CanReadToken(token))
            {
                var issuer = handler.ReadJsonWebToken(token).Issuer?.TrimEnd('/');

                if (IsAuthority(issuer, configuration["IdentityServer:Authority"]))
                {
                    return IdentityServerScheme;
                }

                if (IsAuthority(issuer, configuration["Keycloak:Authority"]))
                {
                    return KeycloakScheme;
                }
            }
        }

        return JwtBearerDefaults.AuthenticationScheme;
    }

    private static bool IsAuthority(string? issuer, string? authority)
        => !string.IsNullOrEmpty(authority)
           && string.Equals(issuer, authority.TrimEnd('/'), StringComparison.Ordinal);
}