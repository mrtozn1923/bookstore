namespace Bookstore.Shipping;

public sealed record QuoteRequest(string DestinationCity, int Parcels, int WeightGrams);

public sealed record CarrierQuote(string QuoteId, decimal Price, string Currency, int EstimatedDays);

// Typed client for the carrier's quote API. Base address, API key, proxy and resilience come from ShippingModule.
public sealed class CarrierClient(HttpClient http)
{
    public async Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync("/v1/quotes", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CarrierQuote>(ct)
            ?? throw new HttpRequestException("The carrier returned an empty quote.");
    }
}
