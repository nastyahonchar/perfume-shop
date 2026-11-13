using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Metrics;

public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _updated;
    private readonly Counter<long> _deleted;
    private readonly Histogram<double> _operationDuration;

    private int _inFlight;

    public CatalogMetrics(IMeterFactory meterFactory, IMemoryCache memoryCache)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>(
            "products.created", unit: "{product}", description: "Number of created products");

        _updated = meter.CreateCounter<long>(
            "products.updated", unit: "{product}", description: "Number of updated products");

        _deleted = meter.CreateCounter<long>(
            "products.deleted", unit: "{product}", description: "Number of deleted products");

        _operationDuration = meter.CreateHistogram<double>(
            "catalog.operation.duration", unit: "ms", description: "Duration of catalog operations");

        meter.CreateObservableGauge(
            "catalog.operations.in_flight",
            () => Volatile.Read(ref _inFlight),
            unit: "{operation}",
            description: "Catalog operations that are being executed right now");

        meter.CreateObservableGauge(
            "catalog.cache.l1.entries",
            () => memoryCache.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
            unit: "{entry}",
            description: "Entries in the in-memory (L1) cache");
    }

    public void ProductCreated(string season) =>
        _created.Add(1, new KeyValuePair<string, object?>("product.season", season));

    public void ProductUpdated(string operation) =>
        _updated.Add(1, new KeyValuePair<string, object?>("operation", operation));

    public void ProductDeleted() => _deleted.Add(1);

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