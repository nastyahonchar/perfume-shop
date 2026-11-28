using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;

namespace Nota.Infra;

public enum ClientFlow
{
    AuthorizationCode,
    ClientCredentials,
    Password
}

public sealed record ClientDefinition(
    string ClientId,
    string DisplayName,
    ClientFlow Flow,
    bool IsPublic,
    string[] RedirectUris,
    string[] Audiences);

// ClientFactory інкапсулює створення клієнтів Keycloak: за описом (ClientDefinition) і типом потоку
// (Authorization Code для Postman та Swagger, Client Credentials для Aggregator) повертає налаштований Client.
// Код, який викликає фабрику, не знає, які саме прапорці потоку треба ввімкнути.
public static class ClientFactory
{
    public static Client Create(Realm realm, ClientDefinition definition) => definition.Flow switch
    {
        ClientFlow.AuthorizationCode => CreateAuthorizationCodeClient(realm, definition),
        ClientFlow.ClientCredentials => CreateClientCredentialsClient(realm, definition),
        ClientFlow.Password => CreatePasswordClient(realm, definition),
        _ => throw new ArgumentOutOfRangeException(nameof(definition), definition.Flow, "Unsupported client flow")
    };

    private static Client CreateAuthorizationCodeClient(Realm realm, ClientDefinition definition) =>
        new(definition.ClientId, new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = definition.ClientId,
            Name = definition.DisplayName,
            AccessType = definition.IsPublic ? "PUBLIC" : "CONFIDENTIAL",
            StandardFlowEnabled = true,
            ImplicitFlowEnabled = false,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = false,
            PkceCodeChallengeMethod = "S256",
            ValidRedirectUris = definition.RedirectUris,
            WebOrigins = new[] { "+" }
        });

    private static Client CreateClientCredentialsClient(Realm realm, ClientDefinition definition) =>
        new(definition.ClientId, new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = definition.ClientId,
            Name = definition.DisplayName,
            AccessType = "CONFIDENTIAL",
            StandardFlowEnabled = false,
            ImplicitFlowEnabled = false,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = true
        });

    private static Client CreatePasswordClient(Realm realm, ClientDefinition definition) =>
        new(definition.ClientId, new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = definition.ClientId,
            Name = definition.DisplayName,
            AccessType = "PUBLIC",
            StandardFlowEnabled = false,
            ImplicitFlowEnabled = false,
            DirectAccessGrantsEnabled = true,
            ServiceAccountsEnabled = false
        });
}