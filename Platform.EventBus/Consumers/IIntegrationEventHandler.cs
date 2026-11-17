using Platform.EventBus.Events;

namespace Platform.EventBus.Consumers;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}

public sealed class NonRetryableEventException(string message, Exception? innerException = null)
    : Exception(message, innerException);