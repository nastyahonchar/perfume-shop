using Orders.Dal.Interfaces;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductPriceChangedHandler(
    IUnitOfWork unitOfWork,
    ILogger<ProductPriceChangedHandler> logger)
    : IdempotentEventHandler<ProductPriceChangedEvent>(unitOfWork, logger)
{
    protected override async Task ApplyAsync(
        ProductPriceChangedEvent e, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.Products.UpdatePriceSnapshotAsync(
            e.ProductId, e.NewPrice, e.OccurredAt.UtcDateTime, cancellationToken);

        switch (result)
        {
            case SnapshotUpdateResult.Updated:
                logger.LogInformation(
                    "Product {ProductId} price snapshot updated from {OldPrice} to {NewPrice}, event {EventId}, correlation {CorrelationId}",
                    e.ProductId, e.OldPrice, e.NewPrice, e.EventId, e.CorrelationId);
                break;
            case SnapshotUpdateResult.Stale:
                logger.LogWarning(
                    "Stale price event ignored for product {ProductId}: event {EventId} is older than the stored data, correlation {CorrelationId}",
                    e.ProductId, e.EventId, e.CorrelationId);
                break;
            default:
                logger.LogWarning(
                    "Product {ProductId} is not in the local Products copy, price event {EventId} skipped, correlation {CorrelationId}",
                    e.ProductId, e.EventId, e.CorrelationId);
                break;
        }
    }
}