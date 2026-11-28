using Pulumi;
using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;
using Pulumi.Keycloak.OpenId.Inputs;

namespace Nota.Infra;

public static class ResourceServerSetup
{
    public static IReadOnlyList<Client> Create(Realm realm, IReadOnlyDictionary<string, Role> roles)
    {
        var servers = new List<Client>();

        foreach (var definition in PermissionMatrix.Servers)
        {
            var client = new Client(definition.ClientId, new ClientArgs
            {
                RealmId = realm.Id,
                ClientId = definition.ClientId,
                Name = definition.DisplayName,
                AccessType = "CONFIDENTIAL",
                ServiceAccountsEnabled = true,
                StandardFlowEnabled = false,
                DirectAccessGrantsEnabled = false,
                Authorization = new ClientAuthorizationArgs
                {
                    PolicyEnforcementMode = "ENFORCING",
                    DecisionStrategy = "AFFIRMATIVE",
                    AllowRemoteResourceManagement = true,
                    KeepDefaults = false
                }
            });

            var scopes = definition.Scopes.ToDictionary(
                scope => scope,
                scope => new ClientAuthorizationScope($"{definition.ClientId}-scope-{scope}", new ClientAuthorizationScopeArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = scope
                }));

            var resourceScopes = new InputList<string>();
            foreach (var scope in scopes.Values)
            {
                resourceScopes.Add(scope.Name);
            }

            var resource = new ClientAuthorizationResource($"{definition.ClientId}-resource", new ClientAuthorizationResourceArgs
            {
                RealmId = realm.Id,
                ResourceServerId = client.ResourceServerId,
                Name = definition.ClientId,
                DisplayName = definition.DisplayName,
                Scopes = resourceScopes
            });

            foreach (var (roleName, grantedScopes) in definition.RolePermissions)
            {
                var policy = new ClientRolePolicy($"{definition.ClientId}-policy-{roleName}", new ClientRolePolicyArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = $"{roleName}-role",
                    DecisionStrategy = "UNANIMOUS",
                    Logic = "POSITIVE",
                    Type = "role",
                    Roles = new[]
                    {
                        new ClientRolePolicyRoleArgs { Id = roles[roleName].Id, Required = false }
                    }
                });

                var scopeIds = new InputList<string>();
                foreach (var scopeName in grantedScopes)
                {
                    scopeIds.Add(scopes[scopeName].Id);
                }

                _ = new ClientAuthorizationPermission($"{definition.ClientId}-permission-{roleName}", new ClientAuthorizationPermissionArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = $"{roleName}-{definition.ClientId}-permission",
                    DecisionStrategy = "UNANIMOUS",
                    Type = "scope",
                    Policies = new[] { policy.Id },
                    Resources = new[] { resource.Id },
                    Scopes = scopeIds
                });
            }

            servers.Add(client);
        }

        return servers;
    }
}