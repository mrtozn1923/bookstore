namespace Bookstore.Shipping.Tests;

// A hand-written stub: the last handler in the chain, so it derives from HttpMessageHandler
public sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        return respond(request, ct);
    }
}
