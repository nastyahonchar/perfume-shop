namespace Platform.EventBus;

public static class EventBusTopology
{
    public const string ConnectionName = "messaging";

    public const string CatalogExchange = "catalog-events";
    public const string DeadLetterExchange = "catalog-events.dlx";

    public const string RoutingKeyPriceChanged = "product.price_changed";
    public const string RoutingKeyProductRenamed = "product.renamed";
    public const string CatalogBindingKey = "product.*";

    public const string OrdersCatalogSyncQueue = "orders.catalog-sync";
    public const string ReviewsCatalogSyncQueue = "reviews.catalog-sync";

    public static string DeadLetterQueue(string queue) => queue + ".dlq";

    public static readonly string[] CatalogSyncQueues =
    [
        OrdersCatalogSyncQueue,
        ReviewsCatalogSyncQueue
    ];
}