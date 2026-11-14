using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public static class HealthChecksExtensions
{
    public static IHealthChecksBuilder AddGrpcDependency(
        this IHealthChecksBuilder builder, string service, HealthStatus failureStatus)
    {
        var clientName = $"{service}-health";

        builder.Services.AddGrpcClient<Health.HealthClient>(
            clientName, options => options.Address = new Uri($"https://{service}"));

        return builder.Add(new HealthCheckRegistration(
            service,
            serviceProvider => new GrpcDependencyHealthCheck(
                serviceProvider.GetRequiredService<GrpcClientFactory>().CreateClient<Health.HealthClient>(clientName),
                service,
                serviceProvider.GetRequiredService<ILogger<GrpcDependencyHealthCheck>>()),
            failureStatus,
            ["grpc", "downstream", "ready"],
            TimeSpan.FromSeconds(3)));
    }
}