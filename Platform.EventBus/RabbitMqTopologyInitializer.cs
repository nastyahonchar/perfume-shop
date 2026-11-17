using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus;

public sealed class RabbitMqTopologyInitializer : IHostedService
{
    private const int MaxAttempts = 5;

    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    public RabbitMqTopologyInitializer(IConnection connection, ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await RabbitMqTopologyDeclarer.DeclareAsync(channel, cancellationToken);

                _logger.LogInformation(
                    "RabbitMQ topology declared: exchange {Exchange}, queues {Queues}",
                    EventBusTopology.CatalogExchange,
                    string.Join(", ", EventBusTopology.CatalogSyncQueues));
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ topology declaration failed (attempt {Attempt}/{MaxAttempts}), retrying",
                    attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBusTopology(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqTopologyInitializer>();
        return services;
    }
}