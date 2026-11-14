using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace Platform.Aggregator.Resilience;

public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddResilientGrpcClient<TClient>(
        this IServiceCollection services, string address, ResilienceProfile profile)
        where TClient : class
    {
        var builder = services.AddGrpcClient<TClient>(o => o.Address = new Uri(address));

        builder.ConfigureChannel(channel => channel.ServiceConfig = CreateServiceConfig(profile));

#pragma warning disable EXTEXP0001
        builder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        builder.AddStandardResilienceHandler(options => Configure(options, profile));

        return builder;
    }

    private static ServiceConfig CreateServiceConfig(ResilienceProfile profile) => new()
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = profile.GrpcMaxAttempts,
                    InitialBackoff = profile.GrpcInitialBackoff,
                    MaxBackoff = profile.GrpcMaxBackoff,
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                }
            }
        }
    };

    private static void Configure(HttpStandardResilienceOptions options, ResilienceProfile profile)
    {
        options.Retry.MaxRetryAttempts = profile.HttpRetryAttempts;
        options.Retry.Delay = profile.HttpRetryDelay;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        options.Retry.DisableFor(HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete);

        options.CircuitBreaker.FailureRatio = profile.FailureRatio;
        options.CircuitBreaker.MinimumThroughput = profile.MinimumThroughput;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = profile.BreakDuration;

        options.CircuitBreaker.OnOpened = args =>
        {
            ResilienceLog.CircuitOpened(profile.Service, args.BreakDuration);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnHalfOpened = _ =>
        {
            ResilienceLog.CircuitHalfOpened(profile.Service);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = _ =>
        {
            ResilienceLog.CircuitClosed(profile.Service);
            return ValueTask.CompletedTask;
        };

        options.AttemptTimeout.Timeout = profile.AttemptTimeout;
        options.TotalRequestTimeout.Timeout = profile.TotalTimeout;
    }
}

public static class ResilienceLog
{
    private static ILogger _logger = NullLogger.Instance;

    public static void Initialize(ILoggerFactory loggerFactory) =>
        _logger = loggerFactory.CreateLogger("Platform.Aggregator.Resilience");

    public static void CircuitOpened(string service, TimeSpan breakDuration) =>
        _logger.LogError("Circuit breaker for {Service} is open, calls are rejected for {BreakDurationSeconds} s",
            service, breakDuration.TotalSeconds);

    public static void CircuitHalfOpened(string service) =>
        _logger.LogWarning("Circuit breaker for {Service} is half-open, a probe call is allowed", service);

    public static void CircuitClosed(string service) =>
        _logger.LogInformation("Circuit breaker for {Service} is closed again", service);
}