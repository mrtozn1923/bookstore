using Bookstore.Configuration;

namespace Bookstore.Payments;

public sealed record PaymentRequest(int OrderId, decimal Amount, string Currency);

public sealed record PaymentResult(string ChargeId, string Status);

// Bookstore's door to the external payment provider.
public interface IPaymentGateway
{
    Task<PaymentResult> AuthorizeAsync(PaymentRequest request, CancellationToken ct);
}

internal sealed class PaymentGatewayClient(HttpClient http, LastKnownGood<PaymentsOptions> options) : IPaymentGateway
{
    public async Task<PaymentResult> AuthorizeAsync(PaymentRequest request, CancellationToken ct)
    {
        // Read on every call: an incident commander can raise the timeout in switches.json without a restart.
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        using HttpRequestMessage message = new(HttpMethod.Post, "/v1/charges")
        {
            Content = JsonContent.Create(new { request.OrderId, request.Amount, request.Currency })
        };
        message.Headers.Add("Idempotency-Key", $"order-{request.OrderId}");

        using HttpResponseMessage response = await http.SendAsync(message, timeout.Token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResult>(timeout.Token)
            ?? throw new InvalidOperationException("The payment provider returned an empty body.");
    }
}
