namespace Bookstore.Payments;

public sealed record PaymentRequest(int OrderId, decimal Amount, string Currency);

public sealed record PaymentResult(string ChargeId, string Status);

// Bookstore's door to the external payment provider.
public interface IPaymentGateway
{
    Task<PaymentResult> AuthorizeAsync(PaymentRequest request, CancellationToken ct);
}

internal sealed class PaymentGatewayClient(HttpClient http) : IPaymentGateway
{
    public async Task<PaymentResult> AuthorizeAsync(PaymentRequest request, CancellationToken ct)
    {
        using HttpRequestMessage message = new(HttpMethod.Post, "/v1/charges")
        {
            Content = JsonContent.Create(new { request.OrderId, request.Amount, request.Currency })
        };
        message.Headers.Add("Idempotency-Key", $"order-{request.OrderId}");

        using HttpResponseMessage response = await http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResult>(ct)
            ?? throw new InvalidOperationException("The payment provider returned an empty body.");
    }
}
