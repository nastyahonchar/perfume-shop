using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Consumers;

// Консюмер працює як BackgroundService, читає чергу з autoAck: false і обмежує кількість
// неопрацьованих повідомлень через BasicQos (prefetch). Підтвердження (BasicAck) надсилається
// лише після успішної обробки, зупинка хоста передається через stoppingToken.
public sealed class EventConsumer : BackgroundService
{
    public const int MaxRetries = 3;
    private const ushort PrefetchCount = 10;

    private readonly string _queue;
    private readonly IReadOnlyDictionary<string, EventRoute> _routes;
    private readonly IServiceProvider _services;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventConsumer> _logger;

    public EventConsumer(
        string queue,
        IReadOnlyDictionary<string, EventRoute> routes,
        IServiceProvider services,
        IServiceScopeFactory scopeFactory,
        ILogger<EventConsumer> logger)
    {
        _queue = queue;
        _routes = routes;
        _services = services;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consumer for queue {Queue} failed to start, retrying in 5 s", _queue);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var connection = _services.GetRequiredService<IConnection>();

        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await using var retryChannel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        await RabbitMqTopologyDeclarer.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => OnReceivedAsync(channel, retryChannel, args, stoppingToken);

        await channel.BasicConsumeAsync(queue: _queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Consumer started: queue {Queue}, prefetch {Prefetch}, event types {EventTypes}",
            _queue, PrefetchCount, string.Join(", ", _routes.Keys));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnReceivedAsync(
        IChannel channel,
        IChannel retryChannel,
        BasicDeliverEventArgs args,
        CancellationToken stoppingToken)
    {
        var properties = args.BasicProperties;
        var headers = properties.Headers;
        var eventType = properties.Type ?? "unknown";
        var eventId = properties.MessageId ?? "unknown";
        var correlationId = ReadString(headers, EventHeaders.CorrelationId) ?? "unknown";
        var retryCount = ReadInt(headers, EventHeaders.RetryCount);

        // Consumer Activity створюється як дочірня до Producer Activity: батьківський контекст береться
        // з traceparent у заголовках повідомлення, теги messaging.* описують чергу, подію й спробу.
        using var activity = TracePropagation.Source.StartActivity(
            $"{_queue} process",
            ActivityKind.Consumer,
            TracePropagation.Extract(headers));
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", _queue);
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.message.id", eventId);
        activity?.SetTag("messaging.message.type", eventType);
        activity?.SetTag("messaging.rabbitmq.retry_count", retryCount);
        activity?.SetTag("correlation.id", correlationId);

        using var correlationScope = CorrelationScope.Begin(correlationId);
        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["EventId"] = eventId,
            ["EventType"] = eventType,
            ["Queue"] = _queue
        });

        IntegrationEvent? integrationEvent = null;

        try
        {
            if (!_routes.TryGetValue(eventType, out var route))
                throw new NonRetryableEventException($"Unknown event type '{eventType}'");

            try
            {
                integrationEvent = JsonSerializer.Deserialize(args.Body.Span, route.EventType, EventJson.Options)
                    as IntegrationEvent;
            }
            catch (JsonException ex)
            {
                throw new NonRetryableEventException($"Message body is not valid JSON for '{eventType}'", ex);
            }

            if (integrationEvent is null)
                throw new NonRetryableEventException("Message body is empty");

            await using var scope = _scopeFactory.CreateAsyncScope();
            await route.InvokeAsync(scope.ServiceProvider, integrationEvent, stoppingToken);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, CancellationToken.None);

            _logger.LogInformation(
                "Event processed {EventType} {EventId} from {Queue}, attempt {Attempt}",
                eventType, eventId, _queue, retryCount + 1);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (NonRetryableEventException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            // Постійна помилка (невалідний JSON, невідомий тип події) не виправиться повтором,
            // тому повідомлення відхиляється через BasicNack з requeue: false і через DLX потрапляє в DLQ.
            _logger.LogError(ex,
                "Permanent failure, event moved to DLQ: {EventType} {EventId} queue {Queue} correlation {CorrelationId} payload {@IntegrationEvent}",
                eventType, eventId, _queue, correlationId, integrationEvent);

            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await RetryOrDeadLetterAsync(
                channel, retryChannel, args, retryCount, ex,
                eventType, eventId, correlationId, integrationEvent, stoppingToken);
        }
    }

    // Тимчасова помилка: повідомлення публікується в ту саму чергу з заголовком x-retry-count,
    // збільшеним на 1, з експоненційною паузою 0.5 / 1 / 2 с. Після MaxRetries (3) невдалих спроб
    // повідомлення відхиляється через BasicNack з requeue: false і потрапляє в DLQ.
    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        IChannel retryChannel,
        BasicDeliverEventArgs args,
        int retryCount,
        Exception exception,
        string eventType,
        string eventId,
        string correlationId,
        IntegrationEvent? integrationEvent,
        CancellationToken stoppingToken)
    {
        if (retryCount >= MaxRetries)
        {
            _logger.LogError(exception,
                "Retries exhausted ({MaxRetries}), event moved to DLQ: {EventType} {EventId} queue {Queue} correlation {CorrelationId} payload {@IntegrationEvent}",
                MaxRetries, eventType, eventId, _queue, correlationId, integrationEvent);

            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        var nextAttempt = retryCount + 1;
        _logger.LogWarning(exception,
            "Event handling failed, scheduling retry {Retry}/{MaxRetries}: {EventType} {EventId} queue {Queue} correlation {CorrelationId} payload {@IntegrationEvent}",
            nextAttempt, MaxRetries, eventType, eventId, _queue, correlationId, integrationEvent);

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, retryCount)), stoppingToken);

            var headers = new Dictionary<string, object?>(
                args.BasicProperties.Headers ?? new Dictionary<string, object?>())
            {
                [EventHeaders.RetryCount] = nextAttempt
            };

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = args.BasicProperties.ContentType,
                MessageId = args.BasicProperties.MessageId,
                Type = args.BasicProperties.Type,
                Headers = headers
            };

            await retryChannel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queue,
                mandatory: false,
                basicProperties: properties,
                body: args.Body,
                cancellationToken: stoppingToken);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, CancellationToken.None);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception publishException)
        {
            _logger.LogError(publishException,
                "Retry publish failed, message returned to queue: {EventType} {EventId} queue {Queue} correlation {CorrelationId}",
                eventType, eventId, _queue, correlationId);

            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
    }

    private static string? ReadString(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value?.ToString()
        };
    }

    private static int ReadInt(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)
            return 0;

        return value switch
        {
            int number => number,
            long number => (int)number,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => int.TryParse(value.ToString(), out var fallback) ? fallback : 0
        };
    }
}