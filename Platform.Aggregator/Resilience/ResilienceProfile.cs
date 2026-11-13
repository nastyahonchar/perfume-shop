namespace Platform.Aggregator.Resilience;

public sealed record ResilienceProfile(
    string Service,
    bool Critical,
    int HttpRetryAttempts,
    TimeSpan HttpRetryDelay,
    double FailureRatio,
    int MinimumThroughput,
    TimeSpan BreakDuration,
    TimeSpan AttemptTimeout,
    TimeSpan TotalTimeout,
    int GrpcMaxAttempts,
    TimeSpan GrpcInitialBackoff,
    TimeSpan GrpcMaxBackoff,
    TimeSpan CallTimeout)
{
    public DateTime CallDeadline() => DateTime.UtcNow.Add(CallTimeout);
}

public static class ResilienceProfiles
{
    public static readonly ResilienceProfile Orders = new(
        Service: "orders-api",
        Critical: true,
        HttpRetryAttempts: 3,
        HttpRetryDelay: TimeSpan.FromMilliseconds(200),
        FailureRatio: 0.3,
        MinimumThroughput: 10,
        BreakDuration: TimeSpan.FromSeconds(10),
        AttemptTimeout: TimeSpan.FromSeconds(3),
        TotalTimeout: TimeSpan.FromSeconds(10),
        GrpcMaxAttempts: 4,
        GrpcInitialBackoff: TimeSpan.FromMilliseconds(200),
        GrpcMaxBackoff: TimeSpan.FromSeconds(2),
        CallTimeout: TimeSpan.FromSeconds(8));

    public static readonly ResilienceProfile Catalog = new(
        Service: "catalog-api",
        Critical: true,
        HttpRetryAttempts: 3,
        HttpRetryDelay: TimeSpan.FromMilliseconds(200),
        FailureRatio: 0.3,
        MinimumThroughput: 10,
        BreakDuration: TimeSpan.FromSeconds(10),
        AttemptTimeout: TimeSpan.FromSeconds(3),
        TotalTimeout: TimeSpan.FromSeconds(10),
        GrpcMaxAttempts: 4,
        GrpcInitialBackoff: TimeSpan.FromMilliseconds(200),
        GrpcMaxBackoff: TimeSpan.FromSeconds(2),
        CallTimeout: TimeSpan.FromSeconds(8));

    public static readonly ResilienceProfile Reviews = new(
        Service: "reviews-api",
        Critical: false,
        HttpRetryAttempts: 1,
        HttpRetryDelay: TimeSpan.FromMilliseconds(200),
        FailureRatio: 0.7,
        MinimumThroughput: 10,
        BreakDuration: TimeSpan.FromSeconds(30),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalTimeout: TimeSpan.FromSeconds(5),
        GrpcMaxAttempts: 2,
        GrpcInitialBackoff: TimeSpan.FromMilliseconds(100),
        GrpcMaxBackoff: TimeSpan.FromMilliseconds(500),
        CallTimeout: TimeSpan.FromSeconds(3));
}