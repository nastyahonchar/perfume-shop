using Orders.Api.Middleware;
using Orders.Bll.Interfaces;
using Orders.Bll.Mapping;
using Orders.Bll.Services;
using Orders.Dal;
using Orders.Dal.Interfaces;
using Platform.ServiceDefaults;
using Serilog;
using Orders.Api.Grpc;
using Orders.Api.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Api.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

builder.Services.AddHealthChecks()
    .AddCheck(
        "orders-db",
        new SqlServerHealthCheck(connectionString),
        HealthStatus.Unhealthy,
        ["database", "sql", "ready"],
        TimeSpan.FromSeconds(5));

builder.Services.AddGrpcHealthChecks(options =>
    options.Services.MapService("", registration => registration.Tags.Contains("ready")));

builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(2);
    options.Period = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<OrderMetrics>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<OrdersProfile>();
});

builder.Services.AddControllers();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<OrdersGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();