using Orders.Dal.Interfaces;
using Platform.EventBus.Consumers;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

// Обробник відкриває транзакцію, реєструє EventId у таблиці ProcessedEvents (унікальний ключ) і лише
// потім змінює дані. Реєстрація й зміна комітяться разом: або відбувається і те, і інше, або нічого.
// Якщо EventId уже є в таблиці (повторна доставка), зміни пропускаються, а подія підтверджується.
public abstract class IdempotentEventHandler<TEvent>(IUnitOfWork unitOfWork, ILogger logger)
    : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    public async Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(ct: cancellationToken);
        try
        {
            var isNew = await unitOfWork.ProcessedEvents.TryMarkProcessedAsync(
                integrationEvent.EventId, typeof(TEvent).Name, cancellationToken);

            if (!isNew)
            {
                logger.LogInformation(
                    "Duplicate event skipped {EventType} {EventId} correlation {CorrelationId}",
                    typeof(TEvent).Name, integrationEvent.EventId, integrationEvent.CorrelationId);
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return;
            }

            await ApplyAsync(integrationEvent, unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    protected abstract Task ApplyAsync(
        TEvent integrationEvent, IUnitOfWork unitOfWork, CancellationToken cancellationToken);
}