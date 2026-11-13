using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Catalog.Api.HealthChecks;

public sealed class RedisHealthCheck(IServiceProvider services) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var redis = await Task.Run(() => services.GetRequiredService<IConnectionMultiplexer>(), cancellationToken)
                .WaitAsync(cancellationToken);

            var latency = await redis.GetDatabase().PingAsync().WaitAsync(cancellationToken);

            return HealthCheckResult.Healthy($"Redis responded to PING in {latency.TotalMilliseconds:F0} ms");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Redis is unavailable, the service works without cache and reads from the database",
                ex);
        }
    }
}