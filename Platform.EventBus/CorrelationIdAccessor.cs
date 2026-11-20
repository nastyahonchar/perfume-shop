using Microsoft.AspNetCore.Http;

namespace Platform.EventBus;

public interface ICorrelationIdAccessor
{
    string GetOrCreate();
}

public static class CorrelationScope
{
    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string? Value => CurrentValue.Value;

    public static IDisposable Begin(string correlationId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = correlationId;
        return new Restore(previous);
    }

    private sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}

public sealed class CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor) : ICorrelationIdAccessor
{
    private const string HttpHeaderName = "X-Correlation-Id";

    public string GetOrCreate()
    {
        if (!string.IsNullOrWhiteSpace(CorrelationScope.Value))
            return CorrelationScope.Value!;

        var fromRequest = httpContextAccessor.HttpContext?.Request.Headers[HttpHeaderName].ToString();
        return string.IsNullOrWhiteSpace(fromRequest) ? Guid.NewGuid().ToString() : fromRequest;
    }
}