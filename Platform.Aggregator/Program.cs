using System.Text.Json.Serialization;
using Catalog.Grpc;
using Orders.Grpc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Reviews.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddResilientGrpcClient<OrdersService.OrdersServiceClient>("https://orders-api", ResilienceProfiles.Orders);
builder.Services.AddResilientGrpcClient<CatalogService.CatalogServiceClient>("https://catalog-api", ResilienceProfiles.Catalog);
builder.Services.AddResilientGrpcClient<ReviewsService.ReviewsServiceClient>("https://reviews-api", ResilienceProfiles.Reviews);

builder.Services.AddScoped<OrdersClient>();
builder.Services.AddScoped<CatalogClient>();
builder.Services.AddScoped<ReviewsClient>();

builder.Services.AddSingleton<AggregationCache>();
builder.Services.AddScoped<OrderAggregationService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

ResilienceLog.Initialize(app.Services.GetRequiredService<ILoggerFactory>());

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();