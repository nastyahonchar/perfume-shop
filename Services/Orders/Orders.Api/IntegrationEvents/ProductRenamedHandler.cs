using Orders.Dal.Interfaces;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductRenamedHandler(
    IUnitOfWork unitOfWork,
    ILogger<ProductRenamedHandler> logger)
    : IdempotentEventHandler<ProductRenamedEvent>(unitOfWork, logger)
{
    protected override async Task ApplyAsync(
        ProductRenamedEvent e, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.Products.UpdateNameSnapshotAsync(
            e.ProductId, e.NewName, e.OccurredAt.UtcDateTime, cancellationToken);

        switch (result)
        {
            case SnapshotUpdateResult.Updated:
                logger.LogInformation(
                    "Product {ProductId} name snapshot updated from {OldName} to {NewName}, event {EventId}, correlation {CorrelationId}",
                    e.ProductId, e.OldName, e.NewName, e.EventId, e.CorrelationId);
                break;
            case SnapshotUpdateResult.Stale:
                logger.LogWarning(
                    "Stale rename event ignored for product {ProductId}: event {EventId} is older than the stored data, correlation {CorrelationId}",
                    e.ProductId, e.EventId, e.CorrelationId);
                break;
            default:
                logger.LogWarning(
                    "Product {ProductId} is not in the local Products copy, rename event {EventId} skipped, correlation {CorrelationId}",
                    e.ProductId, e.EventId, e.CorrelationId);
                break;
        }
    }
}