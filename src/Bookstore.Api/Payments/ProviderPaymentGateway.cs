using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore.Payments;

// The adapter: the only class that knows the provider's JSON, status codes and error codes.
public sealed class ProviderPaymentGateway(HttpClient httpClient, ILogger<ProviderPaymentGateway> logger) : IPaymentGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        var body = new ProviderChargeRequest(
            Amount: ToMinorUnits(request.Amount),
            Currency: request.Currency,
            Source: request.CardToken,
            Description: $"Bookstore order {request.OrderId}");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("v1/charges", body, Json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Nothing reached the provider (DNS, connection refused, TLS): safe to try again later.
            throw new PaymentGatewayException(PaymentFailureKind.Transient, "Payment provider is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The request left our process; we do not know if the card was charged.
            throw new PaymentGatewayException(PaymentFailureKind.UnknownOutcome, "Payment provider did not answer in time.", ex);
        }

        using (response)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Created or HttpStatusCode.OK => await ReadApprovedAsync(response, cancellationToken),
                HttpStatusCode.PaymentRequired => await ReadDeclinedAsync(response, cancellationToken),
                HttpStatusCode.TooManyRequests => throw new PaymentGatewayException(
                    PaymentFailureKind.Transient, "Payment provider is throttling us.")
                    { RetryAfter = response.Headers.RetryAfter?.Delta },
                HttpStatusCode.ServiceUnavailable => throw new PaymentGatewayException(
                    PaymentFailureKind.Unavailable, "Payment provider is unavailable.")
                    { RetryAfter = response.Headers.RetryAfter?.Delta },
                >= HttpStatusCode.InternalServerError => throw new PaymentGatewayException(
                    PaymentFailureKind.Transient, $"Payment provider returned {(int)response.StatusCode}."),
                _ => throw new PaymentGatewayException(
                    PaymentFailureKind.ContractViolation, $"Unexpected status {(int)response.StatusCode} from payment provider.")
            };
        }
    }

    private async Task<PaymentResult> ReadApprovedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var charge = await response.Content.ReadFromJsonAsync<ProviderCharge>(Json, cancellationToken);
        if (charge is not { Status: "succeeded", Id.Length: > 0 })
        {
            throw new PaymentGatewayException(PaymentFailureKind.ContractViolation, "Charge response is missing id or status.");
        }

        logger.LogInformation("Payment approved by provider: {ProviderPaymentId}", charge.Id);
        return new PaymentResult.Approved(charge.Id);
    }

    private static async Task<PaymentResult> ReadDeclinedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var error = await response.Content.ReadFromJsonAsync<ProviderErrorEnvelope>(Json, cancellationToken);
        var reason = error?.Error.Code switch
        {
            "card_declined" => DeclineReason.CardDeclined,
            "insufficient_funds" => DeclineReason.InsufficientFunds,
            "expired_card" => DeclineReason.ExpiredCard,
            _ => DeclineReason.Other
        };
        return new PaymentResult.Declined(reason);
    }

    private static long ToMinorUnits(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    // Provider contract: kept internal so it never leaks into the domain.
    private sealed record ProviderChargeRequest(long Amount, string Currency, string Source, string Description);

    private sealed record ProviderCharge(string Id, string Status, long Amount, string Currency);

    private sealed record ProviderErrorEnvelope(ProviderError Error);

    private sealed record ProviderError(string Type, string? Code, string Message);
}
