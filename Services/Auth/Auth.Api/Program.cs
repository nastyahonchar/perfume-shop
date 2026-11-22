using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityDb' is missing");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Користувачі й ролі зберігаються в окремій базі IdentityDb (ресурс AppHost), доступ через ApplicationDbContext
// (IdentityDbContext<ApplicationUser>). Пароль мінімум 8 символів, після 5 невдалих входів
// обліковий запис блокується на 15 хвилин (lockout), email має бути унікальним і підтвердженим.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();