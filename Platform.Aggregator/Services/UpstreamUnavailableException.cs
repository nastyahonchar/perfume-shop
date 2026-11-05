namespace Platform.Aggregator.Services;

public class UpstreamUnavailableException(string service, Exception inner)
    : Exception($"{service} is unavailable", inner);