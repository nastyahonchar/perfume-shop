using Pulumi.Keycloak;

namespace Nota.Infra;

public static class RoleSetup
{
    public static Dictionary<string, Role> Create(Realm realm) =>
        PermissionMatrix.Roles.ToDictionary(
            name => name,
            name => new Role($"role-{name}", new RoleArgs
            {
                RealmId = realm.Id,
                Name = name,
                Description = $"Realm role '{name}' of the Nota platform"
            }));
}