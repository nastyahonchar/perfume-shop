using System.Text.Json.Serialization;
using Catalog.Grpc;
using Orders.Grpc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Reviews.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o => o.Address = new Uri("https://orders-api"));
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("https://catalog-api"));
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o => o.Address = new Uri("https://reviews-api"));

builder.Services.AddScoped<OrdersClient>();
builder.Services.AddScoped<CatalogClient>();
builder.Services.AddScoped<ReviewsClient>();

builder.Services.AddScoped<OrderAggregationService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();