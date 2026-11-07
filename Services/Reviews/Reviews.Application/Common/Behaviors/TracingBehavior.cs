using System.Diagnostics;
using MediatR;

namespace Reviews.Application.Common.Behaviors;

public static class ReviewsTelemetry
{
    public const string SourceName = "Reviews.Application";

    public static readonly ActivitySource Source = new(SourceName);
}

public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = ReviewsTelemetry.Source.StartActivity(requestName);
        activity?.SetTag("mediatr.request", requestName);

        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
    }
}