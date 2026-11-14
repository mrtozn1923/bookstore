using Bookstore.FakeProviders;
using Bookstore.ResilienceLab.Generated.LegacyCarrier;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Refit;

namespace Bookstore.ResilienceLab.Demos;

// Refit: we write the interface, the source generator writes the implementation.
public interface ILegacyCarrierApi
{
    [Get("/api/v2/quotes")]
    Task<RefitQuote> GetQuoteAsync(string from, string to, int weight, CancellationToken cancellationToken = default);
}

public sealed record RefitQuote(
    [property: System.Text.Json.Serialization.JsonPropertyName("QUOTE_ID")] string? QuoteId,
    [property: System.Text.Json.Serialization.JsonPropertyName("AMT")] string? Amount,
    [property: System.Text.Json.Serialization.JsonPropertyName("CUR")] string? Currency);

public static class ClientsDemo
{
    public static async Task RunAsync()
    {
        using var carrier = FakeLegacyCarrier.Start();
        var baseAddress = new Uri(carrier.Url!);

        var services = new ServiceCollection();
        services.AddRefitGeneratedClient<ILegacyCarrierApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseAddress);
        services.AddHttpClient("kiota-carrier", c => c.BaseAddress = baseAddress);
        await using var provider = services.BuildServiceProvider();

        var refit = provider.GetRequiredService<ILegacyCarrierApi>();
        var kiota = new LegacyCarrierApiClient(new HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(),
            httpClient: provider.GetRequiredService<IHttpClientFactory>().CreateClient("kiota-carrier"))
        {
            BaseUrl = carrier.Url
        });

        foreach (var mode in new[] { FakeMode.Normal, FakeMode.SchemaChanged })
        {
            FakeLegacyCarrier.Configure(carrier, mode);
            Console.WriteLine($"--- carrier mode: {mode}");

            var r = await refit.GetQuoteAsync("34000", "06100", 850);
            Console.WriteLine($"Refit : QUOTE_ID={r.QuoteId}, AMT={r.Amount ?? "null"}");

            var k = await kiota.Api.V2.Quotes.GetAsync(q =>
            {
                q.QueryParameters.From = "34000";
                q.QueryParameters.To = "06100";
                q.QueryParameters.Weight = 850;
            });
            Console.WriteLine($"Kiota : QUOTE_ID={k?.QUOTEID}, AMT={k?.AMT ?? "null"}, " +
                              $"unknown fields=[{string.Join(", ", k?.AdditionalData.Keys ?? [])}]");
        }
    }
}
