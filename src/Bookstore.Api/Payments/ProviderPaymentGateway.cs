using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Bookstore.Payments;

// The adapter: the only class that knows the provider's JSON, status codes and error codes.
public sealed class ProviderPaymentGateway(
    [FromKeyedServices(ProviderPaymentGateway.ClientName)] HttpClient httpClient,
    ILogger<ProviderPaymentGateway> logger) : IPaymentGateway
{
    public const string ClientName = "payment-provider";
    public const string IdempotencyKeyHeader = "Idempotency-Key";

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
            Description: $"Bookstore order {request.OrderId}",
            MerchantReference: request.Reference);

        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/charges")
        {
            Content = JsonContent.Create(body, options: Json)
        };
        // Same key on every attempt for this payment; the draft defines the value as a quoted string.
        message.Headers.Add(IdempotencyKeyHeader, $"\"{request.IdempotencyKey}\"");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Nothing reached the provider (DNS, connection refused, TLS): safe to try again later.
            throw new PaymentGatewayException(PaymentFailureKind.Transient, "Payment provider is unreachable.", ex);
        }
        catch (BrokenCircuitException ex)
        {
            // The circuit is open: the request never left our process.
            throw new PaymentGatewayException(PaymentFailureKind.Unavailable, "Payment provider circuit is open.", ex)
                { RetryAfter = ex.RetryAfter };
        }
        catch (TimeoutRejectedException ex)
        {
            // The resilience pipeline gave up; the request may have reached the provider.
            throw new PaymentGatewayException(PaymentFailureKind.UnknownOutcome, "Payment provider did not answer in time.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The request left our process; we do not know if the card was charged.
            throw new PaymentGatewayException(PaymentFailureKind.UnknownOutcome, "Payment provider did not answer in time.", ex);
        }

        using (response)
        {
            if (response.Headers.Contains("Idempotent-Replayed"))
            {
                logger.LogInformation("Provider replayed the stored response for key {IdempotencyKey}", request.IdempotencyKey);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Created or HttpStatusCode.OK => await ReadApprovedAsync(response, cancellationToken),
                // The first request with this key is still running at the provider.
                HttpStatusCode.Conflict => throw new PaymentGatewayException(
                    PaymentFailureKind.UnknownOutcome, "Payment with this key is still being processed."),
                // Same key, different body: a bug on our side, never retry.
                HttpStatusCode.UnprocessableEntity => throw new PaymentGatewayException(
                    PaymentFailureKind.ContractViolation, "Idempotency-Key was reused with a different request."),
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

    public async Task<PaymentResult.Approved?> FindChargeAsync(string reference, CancellationToken cancellationToken)
    {
        // GET is safe: the pipeline retries it like any other read.
        var list = await httpClient.GetFromJsonAsync<ProviderChargeList>(
            $"v1/charges?merchant_reference={Uri.EscapeDataString(reference)}", Json, cancellationToken);
        var charge = list?.Data.FirstOrDefault(c => c.Status == "succeeded");
        return charge is null ? null : new PaymentResult.Approved(charge.Id);
    }

    // Retry POST only when it carries an Idempotency-Key; a 409 means "still running, ask again later".
    public static bool ShouldRetry(Outcome<HttpResponseMessage> outcome, HttpRequestMessage? request)
    {
        if (request is null || (request.Method == HttpMethod.Post && !request.Headers.Contains(IdempotencyKeyHeader)))
        {
            return false;
        }

        return HttpClientResiliencePredicates.IsTransient(outcome)
            || outcome.Result?.StatusCode == HttpStatusCode.Conflict;
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
    private sealed record ProviderChargeRequest(long Amount, string Currency, string Source, string Description, string MerchantReference);

    private sealed record ProviderCharge(string Id, string Status, long Amount, string Currency);

    private sealed record ProviderChargeList(List<ProviderCharge> Data);

    private sealed record ProviderErrorEnvelope(ProviderError Error);

    private sealed record ProviderError(string Type, string? Code, string Message);
}
