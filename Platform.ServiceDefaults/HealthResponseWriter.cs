using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.ServiceDefaults;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report, bool includeErrors)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                tags = entry.Value.Tags,
                error = includeErrors ? entry.Value.Exception?.Message : null
            })
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}