using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;

namespace Nota.Infra;

// WithDefaultScopes задає повний набір default client scopes (ClientDefaultScopes керує ним авторитетно,
// тому стандартні scopes перелічені явно), а WithAudience додає audience mapper, який кладе resource server
// у claim aud токена. Обидва методи повертають Client, тому викликаються ланцюжком після ClientFactory.Create.
public static class ClientExtensions
{
    private static readonly string[] StandardScopes = ["basic", "acr", "profile", "email", "roles", "web-origins"];

    public static Client WithDefaultScopes(this Client client, Realm realm, string resourceName, params string[] extraScopes)
    {
        _ = new ClientDefaultScopes($"{resourceName}-default-scopes", new ClientDefaultScopesArgs
        {
            RealmId = realm.Id,
            ClientId = client.Id,
            DefaultScopes = StandardScopes.Concat(extraScopes).ToArray()
        });

        return client;
    }

    public static Client WithAudience(this Client client, Realm realm, string resourceName, string audienceClientId)
    {
        _ = new AudienceProtocolMapper($"{resourceName}-audience-{audienceClientId}", new AudienceProtocolMapperArgs
        {
            RealmId = realm.Id,
            ClientId = client.Id,
            Name = $"audience-{audienceClientId}",
            IncludedClientAudience = audienceClientId,
            AddToAccessToken = true,
            AddToIdToken = false
        });

        return client;
    }

    public static Client WithAudiences(this Client client, Realm realm, string resourceName, IEnumerable<string> audiences)
    {
        foreach (var audience in audiences)
        {
            client.WithAudience(realm, resourceName, audience);
        }

        return client;
    }
}