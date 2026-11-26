using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Platform.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    // Scopes дають fine-grained доступ: окремий дозвіл на читання і на запис для кожного API, а не "все або нічого".
    // orders_api і products_api це загальні scopes для кожного мікросервісу, orders.read / orders.write
    // і products.read / products.write це гранульовані дозволи, за якими сервіси будують політики авторизації.
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("orders_api", "Orders API"),
        new ApiScope("products_api", "Products API"),
        new ApiScope("orders.read", "Read orders and customers"),
        new ApiScope("orders.write", "Create, change and delete orders and customers"),
        new ApiScope("products.read", "Read the perfume catalog"),
        new ApiScope("products.write", "Create, change and delete catalog items")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("orders_api", "Orders API")
        {
            Scopes = { "orders_api", "orders.read", "orders.write" }
        },
        new ApiResource("products_api", "Products API")
        {
            Scopes = { "products_api", "products.read", "products.write" }
        }
    ];

    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "postman",
            ClientName = "Postman",
            AllowedGrantTypes = GrantTypes.Code,
            ClientSecrets = { new Secret("secret".Sha256()) },
            RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "orders_api", "products_api",
                "orders.read", "orders.write", "products.read", "products.write"
            },
            AllowOfflineAccess = true
        },

        new Client
        {
            ClientId = "aggregator_service",
            ClientName = "Aggregator Service",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("aggregator_secret".Sha256()) },
            AllowedScopes = { "orders_api", "products_api", "orders.read", "products.read" }
        },

        new Client
        {
            ClientId = "swagger",
            ClientName = "Swagger UI",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            RedirectUris = { "https://localhost:5001/swagger/oauth2-redirect.html" },
            AllowedCorsOrigins = { "https://localhost:5001" },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "orders_api", "orders.read", "orders.write"
            }
        }
    ];
}