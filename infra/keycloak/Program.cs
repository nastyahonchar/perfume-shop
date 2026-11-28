using Nota.Infra;
using Pulumi;

return await Deployment.RunAsync(() =>
{
    var baseUrl = new Config("keycloak").Require("url").TrimEnd('/');

    var realm = RealmSetup.Create();
    var roles = RoleSetup.Create(realm);
    var resourceServers = ResourceServerSetup.Create(realm, roles);
    var clients = ClientSetup.Create(realm);

    var clientRoles = ClientRoleSetup.CreateRoles(realm, resourceServers);
    ClientRoleSetup.GrantToServiceAccount(
        realm, clients[ClientSetup.AggregatorClientId], ClientSetup.AggregatorClientId,
        resourceServers, clientRoles, ClientRoleSetup.Reader, "orders-api", "products-api");

    UserSetup.Create(realm, roles);

    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = realm.RealmName.Apply(name => $"{baseUrl}/realms/{name}"),
        ["AdminConsoleUrl"] = realm.RealmName.Apply(name => $"{baseUrl}/admin/master/console/#/{name}"),
        ["Roles"] = PermissionMatrix.Roles,
        ["ResourceServers"] = PermissionMatrix.Servers.Select(s => s.ClientId).ToArray(),
        ["ClientIds"] = Output.All(clients.Values.Select(c => c.ClientId)),
        ["ClientSecrets"] = new Dictionary<string, object?>
        {
            [ClientSetup.PostmanClientId] = Output.CreateSecret(clients[ClientSetup.PostmanClientId].ClientSecret),
            [ClientSetup.AggregatorClientId] = Output.CreateSecret(clients[ClientSetup.AggregatorClientId].ClientSecret)
        },
        ["TestUsers"] = UserSetup.Users.Select(u => u.Username).ToArray()
    };
});