using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer.Data;
using Platform.ServiceDefaults;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityServerDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityServerDb' is missing");
var migrationsAssembly = typeof(Program).GetTypeInfo().Assembly.GetName().Name;

builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// IdentityServer працює з окремою базою IdentityServerDb на власному SQL Server (identityserver-sql в AppHost),
// ізольованою від бізнес-мікросервісів. Користувачі беруться з ASP.NET Identity (AddAspNetIdentity),
// конфігурація (clients, resources, scopes) лежить у ConfigurationStore, а токени, grants і ключі в OperationalStore.
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
    .AddAspNetIdentity<ApplicationUser>()
    .AddConfigurationStore(options =>
        options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly)))
    .AddOperationalStore(options =>
        options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly)))
    .AddLicenseSummary();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
    await services.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
    await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await IdentitySeeder.SeedAsync(services);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

// Quickstart UI (Razor Pages із шаблону Duende з підтримкою ASP.NET Identity): сторінки Account/Login, Account/Logout,
// Consent, Grants, Device, Error. Вони потрібні Authorization Code Flow: користувач входить у браузері,
// після чого IdentityServer повертає код авторизації клієнту.
app.MapRazorPages().RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();