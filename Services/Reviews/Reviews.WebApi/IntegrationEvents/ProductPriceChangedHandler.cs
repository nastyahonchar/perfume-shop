using Platform.EventBus.Consumers;
using Platform.EventBus.Events;

namespace Reviews.WebApi.IntegrationEvents;

public sealed class ProductPriceChangedHandler(ILogger<ProductPriceChangedHandler> logger)
    : IIntegrationEventHandler<ProductPriceChangedEvent>
{
    public Task HandleAsync(ProductPriceChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Price event ignored by Reviews (price is not replicated): product {ProductId}, event {EventId}, correlation {CorrelationId}",
            integrationEvent.ProductId, integrationEvent.EventId, integrationEvent.CorrelationId);
        return Task.CompletedTask;
    }
}