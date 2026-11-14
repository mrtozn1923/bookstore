using System.Diagnostics;

namespace Bookstore.Infrastructure;

// Sends the current trace id to every external service so their support team can find our request.
public sealed class CorrelationIdHandler : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-ID";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Activity.Current is { } activity && !request.Headers.Contains(HeaderName))
        {
            request.Headers.Add(HeaderName, activity.TraceId.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
