using Catalog.Api.Middleware;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Seeding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;
using Serilog;
using Catalog.Bll.Caching;
using Catalog.Api.Grpc;
using Catalog.Api.Metrics;
using Catalog.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

builder.Services.AddHealthChecks()
    .AddCheck(
        "catalog-db",
        new SqlServerHealthCheck(connectionString),
        HealthStatus.Unhealthy,
        ["database", "sql", "ready"],
        TimeSpan.FromSeconds(5))
    .AddCheck<RedisHealthCheck>(
        "redis-cache",
        HealthStatus.Degraded,
        ["cache", "redis", "ready"],
        TimeSpan.FromSeconds(3));

builder.Services.AddGrpcHealthChecks(options =>
    options.Services.MapService("", registration => registration.Tags.Contains("ready")));

builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(2);
    options.Period = TimeSpan.FromSeconds(10);
});

builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10_000;
    options.TrackStatistics = true;
});
builder.Services.AddSingleton<CatalogMetrics>();
builder.Services.AddCatalogCaching();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IPerfumeService, PerfumeService>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<CatalogProfile>());
builder.Services.AddValidatorsFromAssemblyContaining<CreatePerfumeDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();

    if (!app.Environment.IsProduction())
        await DbInitializer.SeedAsync(db);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();