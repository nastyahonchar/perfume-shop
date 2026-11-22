using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Data;

public static class IdentitySeeder
{
    // Ролі User і Admin створюються під час старту сервісу. У середовищах, відмінних від Production,
    // додається ще й тестовий адміністратор (підтверджений email), щоб можна було перевірити admin-ендпоінти.
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.User, AppRoles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (environment.IsProduction())
            return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var userName = configuration["Seed:AdminUserName"] ?? "admin";
        var email = configuration["Seed:AdminEmail"] ?? "admin@nota.local";
        var password = configuration["Seed:AdminPassword"] ?? "Admin123!";

        if (await userManager.FindByNameAsync(userName) is not null)
            return;

        var admin = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
    }
}