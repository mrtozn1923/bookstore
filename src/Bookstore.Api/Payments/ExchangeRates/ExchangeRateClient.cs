using System.Net.Http.Json;

namespace Bookstore.Payments.ExchangeRates;

public sealed record ExchangeRate(string Pair, decimal Rate, string Source);

// Read-only GET against a service that runs in two regions: a good fit for hedging.
public sealed class ExchangeRateClient([FromKeyedServices(ExchangeRateClient.ClientName)] HttpClient httpClient)
{
    public const string ClientName = "exchange-rates";

    public async Task<ExchangeRate> GetAsync(string pair, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<ExchangeRate>($"api/rates/{Uri.EscapeDataString(pair)}", cancellationToken)
        ?? throw new InvalidOperationException("Empty exchange-rate response.");
}
