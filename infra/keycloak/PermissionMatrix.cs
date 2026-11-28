namespace Nota.Infra;

public sealed record ResourceServerDefinition(
    string ClientId,
    string DisplayName,
    string[] Scopes,
    IReadOnlyDictionary<string, string[]> RolePermissions);

public static class PermissionMatrix
{
    public static readonly string[] Roles = ["admin", "manager", "user", "guest"];

    private static readonly string[] OrdersScopes =
        ["orders:read", "orders:create", "orders:update", "orders:delete", "orders:approve"];

    private static readonly string[] ProductsScopes =
        ["products:read", "products:create", "products:update", "products:delete", "products:publish"];

    private static readonly string[] PaymentsScopes =
        ["payments:read", "payments:process", "payments:refund"];

    // Матриця "роль -> scopes на кожному Resource Server" з таблиць 6 і 7 методички:
    //   admin: усі scopes на всіх трьох серверах;
    //   manager: orders (read, update, approve), products (read, update, publish), payments нічого;
    //   user: orders (read, create), products (read), payments нічого;
    //   guest: лише products (read).
    // payments-api свідомо дає permissions тільки ролі admin, бо платежі це найчутливіший ресурс.
    public static readonly ResourceServerDefinition[] Servers =
    [
        new("orders-api", "Orders API", OrdersScopes, new Dictionary<string, string[]>
        {
            ["admin"] = OrdersScopes,
            ["manager"] = ["orders:read", "orders:update", "orders:approve"],
            ["user"] = ["orders:read", "orders:create"]
        }),

        new("products-api", "Products API", ProductsScopes, new Dictionary<string, string[]>
        {
            ["admin"] = ProductsScopes,
            ["manager"] = ["products:read", "products:update", "products:publish"],
            ["user"] = ["products:read"],
            ["guest"] = ["products:read"]
        }),

        new("payments-api", "Payments API", PaymentsScopes, new Dictionary<string, string[]>
        {
            ["admin"] = PaymentsScopes
        })
    ];
}