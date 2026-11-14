using MongoDB.Driver;
using Platform.ServiceDefaults;
using Reviews.Application;
using Reviews.Application.Reviews.Mapping;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.Middleware;
using Serilog;
using Reviews.WebApi.Grpc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Reviews.WebApi.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var reviewsConnection = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrWhiteSpace(reviewsConnection))
{
    builder.Configuration["MongoDb:ConnectionString"] = reviewsConnection;

    var databaseName = MongoUrl.Create(reviewsConnection).DatabaseName;
    if (!string.IsNullOrWhiteSpace(databaseName))
        builder.Configuration["MongoDb:DatabaseName"] = databaseName;
}

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>(
        "reviews-db",
        HealthStatus.Unhealthy,
        ["database", "mongodb", "ready"],
        TimeSpan.FromSeconds(5));

builder.Services.AddGrpcHealthChecks(options =>
    options.Services.MapService("", registration => registration.Tags.Contains("ready")));

builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(2);
    options.Period = TimeSpan.FromSeconds(10);
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ReviewProfile).Assembly));

builder.Services.AddControllers();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
    await app.Services.SeedDatabaseAsync();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();