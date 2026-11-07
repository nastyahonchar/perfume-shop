using MongoDB.Driver;
using Platform.ServiceDefaults;
using Reviews.Application;
using Reviews.Application.Reviews.Mapping;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.Middleware;
using Serilog;

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

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ReviewProfile).Assembly));

builder.Services.AddControllers();

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
app.MapDefaultEndpoints();

app.Run();