var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();