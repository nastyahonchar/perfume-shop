using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 100;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].ToString();
        var generated = false;

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > MaxLength)
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = correlationId;
            generated = true;
        }

        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (generated)
            {
                logger.LogInformation("Generated new CorrelationId {GeneratedCorrelationId}", correlationId);
            }

            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation("Request started {Method} {Path}",
                context.Request.Method, context.Request.Path);
            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("Request finished with {StatusCode} in {Elapsed} ms",
                    context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}