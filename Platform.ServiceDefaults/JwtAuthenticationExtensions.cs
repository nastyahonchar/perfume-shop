using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Platform.ServiceDefaults;

public static class JwtAuthenticationExtensions
{
    // Спільна валідація access token від Auth Service: підпис (HMAC SHA-256), issuer, audience і час життя.
    // Параметри беруться з конфігурації Jwt (змінні Jwt__Issuer, Jwt__Audience, Jwt__SecretKey з AppHost).
    // Сервіс перевіряє токен локально, не звертаючись до Auth Service при кожному запиті.
    public static IServiceCollection AddPlatformJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

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

        services.AddAuthorization();

        return services;
    }
}