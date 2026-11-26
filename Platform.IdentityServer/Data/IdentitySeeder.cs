using Microsoft.AspNetCore.Identity;

namespace Platform.IdentityServer.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(users, "alice", "alice@nota.local");
        await EnsureUserAsync(users, "bob", "bob@nota.local");
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, string userName, string email)
    {
        if (await users.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, "Pass123$");

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Cannot seed user '{userName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}