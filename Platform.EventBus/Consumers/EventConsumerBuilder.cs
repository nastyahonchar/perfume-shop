using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;

namespace Platform.EventBus.Consumers;

public sealed record EventRoute(
    Type EventType,
    Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> InvokeAsync);

public sealed class EventConsumerBuilder(IServiceCollection services)
{
    public Dictionary<string, EventRoute> Routes { get; } = new();

    public EventConsumerBuilder Handle<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();

        Routes[typeof(TEvent).Name] = new EventRoute(
            typeof(TEvent),
            (provider, integrationEvent, cancellationToken) =>
                provider.GetRequiredService<IIntegrationEventHandler<TEvent>>()
                    .HandleAsync((TEvent)integrationEvent, cancellationToken));

        return this;
    }
}

public static class EventConsumerExtensions
{
    public static IServiceCollection AddEventConsumer(
        this IServiceCollection services,
        string queue,
        Action<EventConsumerBuilder> configure)
    {
        var builder = new EventConsumerBuilder(services);
        configure(builder);

        services.AddSingleton<IHostedService>(provider => new EventConsumer(
            queue,
            builder.Routes,
            provider,
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<EventConsumer>>()));

        return services;
    }
}