using Platform.ApiGateway.Middleware;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPlatformJwtAuthentication();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RoutingLoggingMiddleware>();

// Варіант Б: Gateway перевіряє токен (невалідний або відсутній токен відсікається 401 до сервісу) і пересилає
// заголовок Authorization без змін. Кожен сервіс (Orders, Catalog, Payments) валідує токен незалежно і сам
// вирішує, чи вистачає permissions на конкретному endpoint. Gateway не перевіряє audience токена Keycloak
// (Keycloak:ValidateAudience=false), бо через нього проходять токени для різних resource servers.
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();