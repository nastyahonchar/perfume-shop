using Platform.EventBus.Events;

namespace Platform.EventBus;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}