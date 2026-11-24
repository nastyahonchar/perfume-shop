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

// Gateway перевіряє JWT ДО проксіювання: маршрути з політикою "default" без дійсного токена одразу отримують 401,
// запит до сервісу не доходить. Маршрут /auth/** має політику "anonymous" (login і register без токена),
// заголовок Authorization передається далі, тому мікросервіси валідують токен незалежно.
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();