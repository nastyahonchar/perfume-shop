using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;

namespace Nota.Infra;

public static class ClientSetup
{
    public const string PostmanClientId = "postman";
    public const string SwaggerClientId = "swagger";
    public const string AggregatorClientId = "aggregator_service";
    public const string TestsClientId = "nota-tests";

    private static readonly string[] AllResourceServers = PermissionMatrix.Servers.Select(s => s.ClientId).ToArray();

    public static readonly ClientDefinition[] Definitions =
    [
        new(PostmanClientId, "Postman", ClientFlow.AuthorizationCode, IsPublic: false,
            ["https://oauth.pstmn.io/v1/callback"], AllResourceServers),

        new(SwaggerClientId, "Swagger UI", ClientFlow.AuthorizationCode, IsPublic: true,
            ["https://localhost:5001/swagger/oauth2-redirect.html"], AllResourceServers),

        new(AggregatorClientId, "Aggregator service", ClientFlow.ClientCredentials, IsPublic: false,
            [], ["orders-api", "products-api"]),

        new(TestsClientId, "Automated checks (password grant, dev only)", ClientFlow.Password, IsPublic: true,
            [], AllResourceServers)
    ];

    public static IReadOnlyDictionary<string, Client> Create(Realm realm) =>
        Definitions.ToDictionary(
            definition => definition.ClientId,
            definition => ClientFactory.Create(realm, definition)
                .WithDefaultScopes(realm, definition.ClientId)
                .WithAudiences(realm, definition.ClientId, definition.Audiences));
}