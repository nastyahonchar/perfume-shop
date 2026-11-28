using Nota.Infra;
using Pulumi;

return await Deployment.RunAsync(() =>
{
    var baseUrl = new Config("keycloak").Require("url").TrimEnd('/');

    var realm = RealmSetup.Create();
    var roles = RoleSetup.Create(realm);
    ResourceServerSetup.Create(realm, roles);

    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = realm.RealmName.Apply(name => $"{baseUrl}/realms/{name}"),
        ["AdminConsoleUrl"] = realm.RealmName.Apply(name => $"{baseUrl}/admin/master/console/#/{name}"),
        ["Roles"] = PermissionMatrix.Roles,
        ["ResourceServers"] = PermissionMatrix.Servers.Select(s => s.ClientId).ToArray()
    };
});