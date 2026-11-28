using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;

namespace Nota.Infra;

public static class ClientRoleSetup
{
    public const string Reader = "reader";
    public const string Writer = "writer";
    public const string FullAccess = "full-access";

    // На кожному resource server створюються client-level ролі reader і writer та composite роль full-access,
    // яка об'єднує їх обидві. Service account клієнта (Aggregator) отримує потрібну роль через ClientServiceAccountRole,
    // тобто права сервісу групуються ролями на рівні resource server, а не окремими scopes.
    public static IReadOnlyDictionary<(string Server, string Role), Role> CreateRoles(
        Realm realm, IReadOnlyDictionary<string, Client> resourceServers)
    {
        var result = new Dictionary<(string Server, string Role), Role>();

        foreach (var (serverId, server) in resourceServers)
        {
            var reader = CreateRole(realm, server, serverId, Reader, null);
            var writer = CreateRole(realm, server, serverId, Writer, null);

            result[(serverId, Reader)] = reader;
            result[(serverId, Writer)] = writer;
            result[(serverId, FullAccess)] = CreateRole(realm, server, serverId, FullAccess, new[] { reader.Id, writer.Id });
        }

        return result;
    }

    public static void GrantToServiceAccount(
        Realm realm,
        Client serviceClient,
        string resourceName,
        IReadOnlyDictionary<string, Client> resourceServers,
        IReadOnlyDictionary<(string Server, string Role), Role> roles,
        string roleName,
        params string[] serverIds)
    {
        foreach (var serverId in serverIds)
        {
            _ = new ClientServiceAccountRole($"{resourceName}-{serverId}-{roleName}", new ClientServiceAccountRoleArgs
            {
                RealmId = realm.Id,
                ServiceAccountUserId = serviceClient.ServiceAccountUserId,
                ClientId = resourceServers[serverId].Id,
                Role = roles[(serverId, roleName)].Name
            });
        }
    }

    private static Role CreateRole(Realm realm, Client server, string serverId, string name, Pulumi.Output<string>[]? composites)
    {
        var args = new RoleArgs
        {
            RealmId = realm.Id,
            ClientId = server.Id,
            Name = name,
            Description = $"Client role '{name}' of {serverId}"
        };

        if (composites is not null)
        {
            args.CompositeRoles = composites;
        }

        return new Role($"{serverId}-client-role-{name}", args);
    }
}