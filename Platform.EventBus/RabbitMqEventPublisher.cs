using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using RabbitMQ.Client;

namespace Platform.EventBus;

public sealed class RabbitMqEventPublisher(
    IConnection connection,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    // Повідомлення публікується як persistent (DeliveryMode = Persistent) у форматі application/json,
    // MessageId дорівнює EventId, у заголовках передаються тип події, x-correlation-id і traceparent.
    // Канал з publisher confirms: BasicPublishAsync завершується лише після підтвердження брокером.
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var eventType = @event.GetType().Name;

        using var activity = TracePropagation.Source.StartActivity($"{exchange} publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", @event.EventId.ToString());
        activity?.SetTag("messaging.message.type", eventType);
        activity?.SetTag("correlation.id", @event.CorrelationId);

        var headers = new Dictionary<string, object?>
        {
            [EventHeaders.CorrelationId] = @event.CorrelationId
        };
        TracePropagation.Inject(activity ?? Activity.Current, headers);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = eventType,
            Timestamp = new AmqpTimestamp(@event.OccurredAt.ToUnixTimeSeconds()),
            Headers = headers
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), EventJson.Options);

        try
        {
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                cancellationToken);

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Event published {EventType} {EventId} exchange {Exchange} routing key {RoutingKey} correlation {CorrelationId}",
                eventType, @event.EventId, exchange, routingKey, @event.CorrelationId);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex,
                "Failed to publish event {EventType} {EventId} exchange {Exchange} routing key {RoutingKey} correlation {CorrelationId}",
                eventType, @event.EventId, exchange, routingKey, @event.CorrelationId);
            throw;
        }
    }
}