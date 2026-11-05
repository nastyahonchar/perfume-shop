using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Platform.ApiGateway.Middleware;

public class RoutingLoggingMiddleware(RequestDelegate next, ILogger<RoutingLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            var proxy = context.Features.Get<IReverseProxyFeature>();
            var statusCode = context.Response.StatusCode;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            if (proxy is null)
            {
                logger.LogWarning(
                    "{Method} {Path} is not routed: {StatusCode} in {Elapsed} ms, client {ClientIp}",
                    context.Request.Method, context.Request.Path, statusCode,
                    stopwatch.ElapsedMilliseconds, clientIp);
            }
            else
            {
                var routeId = proxy.Route.Config.RouteId;
                var clusterId = proxy.Cluster.Config.ClusterId;
                var destination = proxy.ProxiedDestination?.Model.Config.Address;

                logger.LogInformation(
                    "{Method} {Path} -> route {Route}, cluster {Cluster} ({Destination}): {StatusCode} in {Elapsed} ms, {ResponseBytes} bytes, client {ClientIp}",
                    context.Request.Method, context.Request.Path, routeId, clusterId, destination,
                    statusCode, stopwatch.ElapsedMilliseconds, context.Response.ContentLength, clientIp);

                var error = context.Features.Get<IForwarderErrorFeature>();
                if (error is not null)
                {
                    logger.LogError(error.Exception,
                        "Forwarding to cluster {Cluster} failed: {ForwarderError}",
                        clusterId, error.Error);
                }
            }
        }
    }
}