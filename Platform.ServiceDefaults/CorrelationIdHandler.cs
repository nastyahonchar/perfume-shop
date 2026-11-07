using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults;

public class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var correlationId = accessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, ct);
    }
}