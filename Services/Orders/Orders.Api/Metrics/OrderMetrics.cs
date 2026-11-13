using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Metrics;

public sealed class OrderMetrics
{
    public const string MeterName = "Orders.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _confirmed;
    private readonly Counter<long> _statusChanged;
    private readonly Histogram<double> _operationDuration;

    private int _inFlight;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>(
            "orders.created", unit: "{order}", description: "Number of created orders");

        _confirmed = meter.CreateCounter<long>(
            "orders.confirmed", unit: "{order}", description: "Number of confirmed orders");

        _statusChanged = meter.CreateCounter<long>(
            "orders.status_changed", unit: "{order}", description: "Number of order status changes");

        _operationDuration = meter.CreateHistogram<double>(
            "orders.operation.duration", unit: "ms", description: "Duration of order operations");

        meter.CreateObservableGauge(
            "orders.operations.in_flight",
            () => Volatile.Read(ref _inFlight),
            unit: "{operation}",
            description: "Order operations that are being executed right now");
    }

    public void OrderCreated(string status) =>
        _created.Add(1, new KeyValuePair<string, object?>("order.status", status));

    public void OrderConfirmed() => _confirmed.Add(1);

    public void StatusChanged(string status) =>
        _statusChanged.Add(1, new KeyValuePair<string, object?>("order.status", status));

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        Interlocked.Increment(ref _inFlight);
        var started = Stopwatch.GetTimestamp();
        var success = false;

        try
        {
            var result = await action();
            success = true;
            return result;
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);

            _operationDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
        }
    }

    public async Task MeasureAsync(string operation, Func<Task> action) =>
        await MeasureAsync(operation, async () =>
        {
            await action();
            return true;
        });
}