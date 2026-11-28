using Pulumi.Keycloak;
using Pulumi.Keycloak.Inputs;

namespace Nota.Infra;

public sealed record TestUserDefinition(string Username, string FirstName, string LastName, string Role);

public static class UserSetup
{
    private const string TemporaryPassword = "Temp123$";

    public static readonly TestUserDefinition[] Users =
    [
        new("john.admin", "John", "Admin", "admin"),
        new("john.manager", "John", "Manager", "manager"),
        new("john.user", "John", "User", "user"),
        new("john.guest", "John", "Guest", "guest")
    ];

    // Для кожної ролі (admin, manager, user, guest) створюється користувач з тимчасовим паролем (Temporary = true),
    // підтвердженим email (EmailVerified = true), а роль призначається окремим ресурсом UserRoles.
    public static IReadOnlyList<User> Create(Realm realm, IReadOnlyDictionary<string, Role> roles)
    {
        var created = new List<User>();

        foreach (var definition in Users)
        {
            var user = new User($"user-{definition.Username}", new UserArgs
            {
                RealmId = realm.Id,
                Username = definition.Username,
                Email = $"{definition.Username}@nota.local",
                EmailVerified = true,
                Enabled = true,
                FirstName = definition.FirstName,
                LastName = definition.LastName,
                InitialPassword = new UserInitialPasswordArgs
                {
                    Value = TemporaryPassword,
                    Temporary = true
                }
            });

            _ = new UserRoles($"user-roles-{definition.Username}", new UserRolesArgs
            {
                RealmId = realm.Id,
                UserId = user.Id,
                RoleIds = new[] { roles[definition.Role].Id }
            });

            created.Add(user);
        }

        return created;
    }
}