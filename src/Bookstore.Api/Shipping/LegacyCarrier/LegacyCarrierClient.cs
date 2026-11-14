using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Bookstore.Shipping.LegacyCarrier;

// Hand-written typed client + anti-corruption mapping for the legacy carrier API.
public sealed class LegacyCarrierClient([FromKeyedServices(LegacyCarrierClient.ClientName)] HttpClient httpClient) : IShippingCarrier
{
    public const string ClientName = "legacy-carrier";

    public async Task<ShippingQuote> GetQuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
    {
        var url = $"api/v2/quotes?from={Uri.EscapeDataString(request.FromPostalCode)}" +
                  $"&to={Uri.EscapeDataString(request.ToPostalCode)}&weight={request.WeightGrams}";

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException or BrokenCircuitException or HttpRequestException)
        {
            var kind = ex is BrokenCircuitException ? ShippingFailureKind.Unavailable : ShippingFailureKind.Transient;
            throw new ShippingCarrierException(kind, $"Carrier call failed: {ex.GetType().Name}.", ex);
        }

        using var _ = response;
        if ((int)response.StatusCode >= 500 || (int)response.StatusCode == 429)
        {
            var kind = (int)response.StatusCode == 503 ? ShippingFailureKind.Unavailable : ShippingFailureKind.Transient;
            throw new ShippingCarrierException(kind, $"Carrier returned {(int)response.StatusCode}.");
        }

        response.EnsureSuccessStatusCode();

        LegacyQuote? quote;
        try
        {
            quote = await response.Content.ReadFromJsonAsync<LegacyQuote>(cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new ShippingCarrierException(ShippingFailureKind.ContractViolation, "Carrier quote is not valid JSON.", ex);
        }

        return Map(quote);
    }

    internal static ShippingQuote Map(LegacyQuote? quote)
    {
        if (quote is null || string.IsNullOrEmpty(quote.QuoteId) ||
            !decimal.TryParse(quote.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            throw new ShippingCarrierException(ShippingFailureKind.ContractViolation,
                $"Carrier quote could not be mapped: QUOTE_ID={quote?.QuoteId ?? "null"}, AMT={quote?.Amount ?? "null"}.");
        }

        return new ShippingQuote(quote.QuoteId, price, quote.Currency ?? "TRY", quote.EtaDays);
    }

    internal sealed record LegacyQuote(
        [property: JsonPropertyName("QUOTE_ID")] string? QuoteId,
        [property: JsonPropertyName("AMT")] string? Amount,
        [property: JsonPropertyName("CUR")] string? Currency,
        [property: JsonPropertyName("SVC")] string? ServiceCode,
        [property: JsonPropertyName("ETA_DAYS")] int EtaDays);
}
