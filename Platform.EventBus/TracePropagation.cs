using System.Diagnostics;

namespace Platform.EventBus;

public static class TracePropagation
{
    public const string ActivitySourceName = "Platform.Messaging";

    public static readonly ActivitySource Source = new(ActivitySourceName);

    // Inject записує W3C traceparent/tracestate поточної Activity в заголовки повідомлення,
    // Extract відновлює контекст у консюмері, щоб Consumer Activity стала дочірньою до Producer Activity
    // і весь ланцюг запит -> подія -> обробка був одним trace.
    public static void Inject(Activity? activity, IDictionary<string, object?> headers)
    {
        if (activity is null)
            return;

        DistributedContextPropagator.Current.Inject(activity, headers, static (carrier, key, value) =>
        {
            if (carrier is IDictionary<string, object?> dictionary)
                dictionary[key] = value;
        });
    }

    public static ActivityContext Extract(IDictionary<string, object?>? headers)
    {
        if (headers is null)
            return default;

        DistributedContextPropagator.Current.ExtractTraceIdAndState(
            headers,
            static (object? carrier, string name, out string? value, out IEnumerable<string>? values) =>
            {
                values = null;
                value = null;
                if (carrier is IDictionary<string, object?> dictionary
                    && dictionary.TryGetValue(name, out var raw))
                {
                    value = raw switch
                    {
                        byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                        string text => text,
                        _ => null
                    };
                }
            },
            out var traceId,
            out var traceState);

        return ActivityContext.TryParse(traceId, traceState, isRemote: true, out var context)
            ? context
            : default;
    }
}