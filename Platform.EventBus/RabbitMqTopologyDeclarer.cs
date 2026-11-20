using RabbitMQ.Client;

namespace Platform.EventBus;

public static class RabbitMqTopologyDeclarer
{
    // Топологія описана в коді й створюється при старті сервісу: topic-обмінник catalog-events,
    // черги orders.catalog-sync та reviews.catalog-sync з прив'язкою product.*, dead-letter обмінник
    // catalog-events.dlx і черги *.dlq. Кожна основна черга має x-dead-letter-exchange та
    // x-dead-letter-routing-key. DLX типу direct, щоб повідомлення однієї черги не потрапляло в DLQ іншої.
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(
            exchange: EventBusTopology.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: EventBusTopology.CatalogExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        foreach (var queue in EventBusTopology.CatalogSyncQueues)
        {
            var deadLetterQueue = EventBusTopology.DeadLetterQueue(queue);

            await channel.QueueDeclareAsync(
                queue: deadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: deadLetterQueue,
                exchange: EventBusTopology.DeadLetterExchange,
                routingKey: deadLetterQueue,
                cancellationToken: cancellationToken);

            var arguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = EventBusTopology.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = deadLetterQueue
            };

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queue,
                exchange: EventBusTopology.CatalogExchange,
                routingKey: EventBusTopology.CatalogBindingKey,
                cancellationToken: cancellationToken);
        }
    }
}