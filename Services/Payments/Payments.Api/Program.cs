using Payments.Api.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPlatformJwtAuthentication();

// Payments Service це заглушка платіжного сервісу: платежі зберігаються в пам'яті, реальної платіжної системи немає.
// Доступ до нього має ЛИШЕ роль admin (таблиця 7 і таблиця 10). Політики суворі (KeycloakOnly): токени Auth Service
// та IdentityServer сюди не допускаються, навіть автентифіковані, бо платежі це найчутливіший ресурс системи.
builder.Services.AddKeycloakOnlyPermissionPolicies("payments:read", "payments:process", "payments:refund");

builder.Services.AddSingleton<PaymentStore>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
