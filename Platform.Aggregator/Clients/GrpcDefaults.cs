namespace Platform.Aggregator.Clients;

public static class GrpcDefaults
{
    public static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(5);

    public static DateTime Deadline() => DateTime.UtcNow.Add(CallTimeout);
}