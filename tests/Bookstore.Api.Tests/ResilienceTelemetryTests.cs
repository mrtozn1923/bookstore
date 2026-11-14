using System.Net.Http.Json;
using Bookstore.FakeProviders;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using WireMock.Server;

namespace Bookstore.Api.Tests;

public sealed class ResilienceTelemetryTests : IDisposable
{
    private readonly WireMockServer _payments = FakePaymentProvider.Start();
    private readonly WireMockServer _carrier = FakeLegacyCarrier.Start(mode: FakeMode.Error);

    public void Dispose()
    {
        _payments.Dispose();
        _carrier.Dispose();
    }

    [Fact]
    public async Task Retries_and_fallbacks_show_up_in_polly_metrics()
    {
        // Polly publishes resilience events on the "Polly" meter.
        using var events = new MetricCollector<int>(null, "Polly", "resilience.polly.strategy.events");
        await using var api = new BookstoreApiFactory(_payments, _carrier);

        using var _ = await api.CreateClient().PostAsJsonAsync("/api/orders/1001/checkout",
            new { cardToken = "tok_visa" }, TestContext.Current.CancellationToken);

        foreach (var m in events.GetMeasurementSnapshot())
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{m.Tags["event.name"]}: {string.Join(", ", m.Tags.Where(t => t.Key is "pipeline.name" or "strategy.name" or "event.severity").Select(t => $"{t.Key}={t.Value}"))}");
        }

        var snapshot = events.GetMeasurementSnapshot();
        Assert.Equal(2, snapshot.Count(m => m.Tags["event.name"] is "OnRetry"));
        Assert.Equal(1, snapshot.Count(m => m.Tags["event.name"] is "OnFallback"));
    }
}
