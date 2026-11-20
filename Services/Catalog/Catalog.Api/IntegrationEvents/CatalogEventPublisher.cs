using Catalog.Bll.Interfaces;
using Platform.EventBus;
using Platform.EventBus.Events;

namespace Catalog.Api.IntegrationEvents;

public sealed class CatalogEventPublisher(
    IEventPublisher publisher,
    ICorrelationIdAccessor correlation,
    ILogger<CatalogEventPublisher> logger) : ICatalogEventPublisher
{
    public Task PublishPriceChangedAsync(int productId, string productName, decimal oldPrice, decimal newPrice)
    {
        var integrationEvent = new ProductPriceChangedEvent(productId, productName, oldPrice, newPrice)
        {
            CorrelationId = correlation.GetOrCreate()
        };
        return PublishSafelyAsync(integrationEvent, productId, EventBusTopology.RoutingKeyPriceChanged);
    }

    public Task PublishProductRenamedAsync(int productId, string oldName, string newName)
    {
        var integrationEvent = new ProductRenamedEvent(productId, oldName, newName)
        {
            CorrelationId = correlation.GetOrCreate()
        };
        return PublishSafelyAsync(integrationEvent, productId, EventBusTopology.RoutingKeyProductRenamed);
    }

    private async Task PublishSafelyAsync<TEvent>(TEvent integrationEvent, int productId, string routingKey)
        where TEvent : IntegrationEvent
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await publisher.PublishAsync(integrationEvent, EventBusTopology.CatalogExchange, routingKey, timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Event {EventType} {EventId} for product {ProductId} was not published, but the change is already saved. Correlation {CorrelationId}",
                typeof(TEvent).Name, integrationEvent.EventId, productId, integrationEvent.CorrelationId);
        }
    }
}