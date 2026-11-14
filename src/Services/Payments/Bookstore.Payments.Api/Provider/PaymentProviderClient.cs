using System.Net;
using System.Text.Json;
using Polly.Timeout;

namespace Bookstore.Payments.Provider;

// Anti-corruption layer: the provider's JSON, status strings and error codes stop here.
// The rest of Payments sees only ChargeOutcome.
public abstract record ChargeOutcome
{
    // The provider answered. Pending means "accepted, the result comes as a webhook".
    public sealed record Answered(string ChargeId, ChargeState State, string? FailureReason) : ChargeOutcome;

    // Timeout, 5xx or a broken connection: the card may or may not have been charged.
    public sealed record Unknown(string Reason) : ChargeOutcome;
}

public enum ChargeState
{
    Pending,
    Succeeded,
    Failed
}

public sealed class PaymentProviderClient(HttpClient http, ILogger<PaymentProviderClient> logger)
{
    public async Task<ChargeOutcome> ChargeAsync(string idempotencyKey, Guid orderId, decimal amount, string currency, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/charges")
        {
            Content = JsonContent.Create(new ProviderChargeRequest(amount, currency, orderId.ToString()), options: ProviderJson.Options)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        try
        {
            using HttpResponseMessage response = await http.SendAsync(request, ct);
            if ((int)response.StatusCode >= 500)
            {
                return new ChargeOutcome.Unknown($"provider returned {(int)response.StatusCode}");
            }
            // 4xx means our request is wrong: retrying the same request would fail the same way.
            response.EnsureSuccessStatusCode();
            ProviderCharge charge = (await response.Content.ReadFromJsonAsync<ProviderCharge>(ProviderJson.Options, ct))!;
            return ToOutcome(charge);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("Charge {IdempotencyKey} has an unknown result: {Error}", idempotencyKey, ex.GetType().Name);
            return new ChargeOutcome.Unknown(ex.GetType().Name);
        }
    }

    // Reconciliation: what does the provider know about this order? Null when it has never seen it.
    public async Task<ChargeOutcome.Answered?> FindByOrderAsync(Guid orderId, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.GetAsync($"/v1/charges?reference={orderId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return ToOutcome((await response.Content.ReadFromJsonAsync<ProviderCharge>(ProviderJson.Options, ct))!);
    }

    public async Task RefundAsync(string idempotencyKey, string chargeId, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/refunds")
        {
            Content = JsonContent.Create(new ProviderRefundRequest(chargeId), options: ProviderJson.Options)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    internal static ChargeOutcome.Answered ToOutcome(ProviderCharge charge) => new(
        charge.Id,
        charge.Status switch
        {
            "succeeded" => ChargeState.Succeeded,
            "failed" => ChargeState.Failed,
            _ => ChargeState.Pending
        },
        charge.FailureCode);
}

// The provider's wire format. Internal: nothing outside the Provider folder uses these types.
internal sealed record ProviderChargeRequest(decimal Amount, string Currency, string Reference);

internal sealed record ProviderRefundRequest(string ChargeId);

internal sealed record ProviderCharge(string Id, string Reference, decimal Amount, string Currency, string Status, string? FailureCode);

internal sealed record ProviderWebhook(string Type, ProviderCharge Data);

internal static class ProviderJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}
