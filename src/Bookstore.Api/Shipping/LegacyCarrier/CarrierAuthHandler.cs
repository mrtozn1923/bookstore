using System.Net.Http.Headers;

namespace Bookstore.Shipping.LegacyCarrier;

// Transient handler; the token cache it depends on is a singleton, so no scope is captured.
public sealed class CarrierAuthHandler(CarrierTokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
