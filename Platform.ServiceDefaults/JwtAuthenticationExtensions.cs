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

    // Спільна валідація access token. Схема "Platform" дивиться на issuer токена й вибирає перевірку:
    // токени Auth Service перевіряються за підписом HMAC SHA-256 (Jwt:Issuer, Jwt:Audience, Jwt:SecretKey),
    // токени Duende IdentityServer через Authority (IdentityServer:Authority): ключі підпису завантажуються
    // автоматично з discovery document, а Audience (IdentityServer:Audience) це ім'я ApiResource сервісу.
    // Сервіс перевіряє токен локально, не звертаючись до Auth Service чи IdentityServer при кожному запиті.
    public static IServiceCollection AddPlatformJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = PlatformScheme;
            options.DefaultAuthenticateScheme = PlatformScheme;
            options.DefaultChallengeScheme = PlatformScheme;
        })
            .AddPolicyScheme(PlatformScheme, "Auth Service or IdentityServer",
                options => options.ForwardDefaultSelector = SelectScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { })
            .AddJwtBearer(IdentityServerScheme, _ => { });

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
            {
                var section = configuration.GetSection("IdentityServer");

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
            });

        services.AddAuthorization();

        return services;
    }

    private static string SelectScheme(HttpContext context)
    {
        var authority = context.RequestServices.GetRequiredService<IConfiguration>()["IdentityServer:Authority"]?.TrimEnd('/');
        var header = context.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(authority) && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header["Bearer ".Length..].Trim();
            var handler = new JsonWebTokenHandler();

            if (handler.CanReadToken(token)
                && string.Equals(handler.ReadJsonWebToken(token).Issuer?.TrimEnd('/'), authority, StringComparison.Ordinal))
            {
                return IdentityServerScheme;
            }
        }

        return JwtBearerDefaults.AuthenticationScheme;
    }
}