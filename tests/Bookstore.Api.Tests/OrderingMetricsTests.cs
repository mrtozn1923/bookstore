using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Bookstore.Api.Tests;

// The API host is shared by all tests, so its meters see every order placed in parallel tests.
// Telemetry tests run alone; otherwise a measurement of another test lands in the snapshot.
[CollectionDefinition(DisableParallelization = true)]
public sealed class MetricsCollection;

[Collection(typeof(MetricsCollection))]
public sealed class OrderingMetricsTests(BookstoreApiFactory factory)
{
    private const string Emma = "9780141439587";

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_placed_order_is_counted_and_its_checkout_duration_is_recorded()
    {
        // The collector listens to the meter created by the app's own IMeterFactory: other tests' meters do not leak in.
        IMeterFactory meterFactory = factory.Services.GetRequiredService<IMeterFactory>();
        using MetricCollector<long> placed = new(meterFactory, OrderingMetrics.MeterName, "bookstore.orders.placed");
        using MetricCollector<double> duration = new(meterFactory, OrderingMetrics.MeterName, "bookstore.checkout.duration");

        HttpResponseMessage cart = await client.PostAsync("/api/carts", null, ct);
        Guid cartId = (await cart.Content.ReadFromJsonAsync<CartView>(ct))!.Id;
        await client.PutAsJsonAsync($"/api/carts/{cartId}/items/{Emma}", new SetQuantityRequest(1), ct);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Ada Lovelace", "ada@example.com"), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CollectedMeasurement<long> order = Assert.Single(placed.GetMeasurementSnapshot());
        Assert.Equal(1, order.Value);
        Assert.Equal("retail", order.Tags["bookstore.order.type"]);

        CollectedMeasurement<double> checkout = Assert.Single(duration.GetMeasurementSnapshot());
        Assert.Equal("placed", checkout.Tags["bookstore.checkout.outcome"]);
        Assert.InRange(checkout.Value, 0, 5);
    }
}
