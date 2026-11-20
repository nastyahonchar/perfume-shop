using Platform.EventBus.Consumers;
using Platform.EventBus.Events;
using Reviews.Domain.Interfaces;

namespace Reviews.WebApi.IntegrationEvents;

public sealed class ProductRenamedHandler(
    IReviewRepository reviews,
    IProcessedEventRepository processedEvents,
    ILogger<ProductRenamedHandler> logger) : IIntegrationEventHandler<ProductRenamedEvent>
{
    // Журнал оброблених подій зберігається в колекції processedEvents (_id = EventId, унікальний ключ).
    // Порядок: перевірити журнал, виконати зміну (вона сама ідемпотентна), записати подію в журнал.
    // MongoDB у цьому стенді працює без транзакцій, тому при збої між зміною і записом
    // повторна доставка лише ще раз виставить те саме значення. Дублікат пропускається без змін.
    public async Task HandleAsync(ProductRenamedEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (await processedEvents.ExistsAsync(integrationEvent.EventId, cancellationToken))
        {
            logger.LogInformation(
                "Duplicate event skipped {EventType} {EventId} correlation {CorrelationId}",
                nameof(ProductRenamedEvent), integrationEvent.EventId, integrationEvent.CorrelationId);
            return;
        }

        var modified = await reviews.UpdateProductNameAsync(
            integrationEvent.ProductId,
            integrationEvent.NewName,
            integrationEvent.OccurredAt.UtcDateTime,
            cancellationToken);

        await processedEvents.TryMarkAsync(
            integrationEvent.EventId, nameof(ProductRenamedEvent), cancellationToken);

        logger.LogInformation(
            "Product {ProductId} renamed from {OldName} to {NewName}: {ModifiedReviews} reviews updated, event {EventId}, correlation {CorrelationId}",
            integrationEvent.ProductId, integrationEvent.OldName, integrationEvent.NewName,
            modified, integrationEvent.EventId, integrationEvent.CorrelationId);
    }
}