using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public sealed class GrpcDependencyHealthCheck(
    Health.HealthClient client,
    string service,
    ILogger<GrpcDependencyHealthCheck> logger) : IHealthCheck
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await client.CheckAsync(
                new HealthCheckRequest(),
                deadline: DateTime.UtcNow.Add(Deadline),
                cancellationToken: cancellationToken);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy($"{service} is serving")
                : new HealthCheckResult(context.Registration.FailureStatus, $"{service} status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "{Service} health check failed with gRPC status {GrpcStatus}",
                service, ex.StatusCode);

            return new HealthCheckResult(
                context.Registration.FailureStatus, $"{service} is unavailable ({ex.StatusCode})", ex);
        }
    }
}