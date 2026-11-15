using System.Net;

namespace Bookstore.Api.Integrations;

public sealed class PaymentOptions
{
    public Uri BaseUrl { get; set; } = new("https://payments.invalid/");
    public string ApiKey { get; set; } = "";
}

public sealed record ChargeRequest(string OrderReference, decimal Amount, string Currency);

public sealed record ChargeResponse(string ChargeId, string Status);

public sealed record PaymentResult(bool Approved, string? ChargeId, string? DeclineReason);

public sealed class PaymentClient(HttpClient httpClient)
{
    public async Task<PaymentResult> ChargeAsync(ChargeRequest request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("v1/charges", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.PaymentRequired)
        {
            return new PaymentResult(false, null, "card_declined");
        }

        response.EnsureSuccessStatusCode();
        ChargeResponse? body = await response.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken);
        return new PaymentResult(true, body!.ChargeId, null);
    }
}
