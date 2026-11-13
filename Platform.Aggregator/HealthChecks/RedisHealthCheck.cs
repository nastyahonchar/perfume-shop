using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Platform.Aggregator.HealthChecks;

public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await redis.GetDatabase().PingAsync().WaitAsync(cancellationToken);

            return HealthCheckResult.Healthy($"Redis responded to PING in {latency.TotalMilliseconds:F0} ms");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Redis is unavailable, the aggregation cache is skipped and data is read from the services",
                ex);
        }
    }
}