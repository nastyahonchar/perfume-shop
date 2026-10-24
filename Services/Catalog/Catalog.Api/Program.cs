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
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IPerfumeService, PerfumeService>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<CatalogProfile>());
builder.Services.AddValidatorsFromAssemblyContaining<CreatePerfumeDtoValidator>();

builder.Services.AddControllers();

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

app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? ctx.TraceIdentifier;
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("RequestPath", ctx.Request.Path.Value))
    {
        await next();
    }
});

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();