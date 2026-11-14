using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;

namespace Bookstore.Shipping.Tests;

public sealed class ParcelTrackingClientTests
{
    private const string Parcel = "https://carrier.example.com/api/v2/parcels/BS2026000042TR";

    // A new handler per test: When/Expect definitions never leak into another test
    private readonly MockHttpMessageHandler carrier = new();

    private ParcelTrackingClient CreateClient() =>
        new(new HttpClient(carrier) { BaseAddress = new Uri("https://carrier.example.com/api/v2/") });

    [Fact]
    public async Task Delivered_parcel_is_read_with_its_events()
    {
        carrier.When(HttpMethod.Get, Parcel).Respond("application/json", CarrierResponses.Delivered);

        ParcelStatus? status = await CreateClient().GetStatusAsync("BS2026000042TR", TestContext.Current.CancellationToken);

        Assert.NotNull(status);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 15, 42, 0, TimeSpan.Zero), status.DeliveredAtUtc);
        Assert.Equal(["Picked up", "Delivered to recipient"], status.Events.Select(e => e.Description));
    }

    [Fact]
    public async Task Unknown_parcel_returns_null()
    {
        carrier.When(HttpMethod.Get, Parcel).Respond(HttpStatusCode.NotFound);

        ParcelStatus? status = await CreateClient().GetStatusAsync("BS2026000042TR", TestContext.Current.CancellationToken);

        Assert.Null(status);
    }

    [Fact]
    public async Task Tracking_number_is_escaped_in_the_path()
    {
        MockedRequest escaped = carrier.When("https://carrier.example.com/api/v2/parcels/BS%2042%2FTR")
            .Respond(HttpStatusCode.NotFound);

        await CreateClient().GetStatusAsync("BS 42/TR", TestContext.Current.CancellationToken);

        Assert.Equal(1, carrier.GetMatchCount(escaped));
    }

    [Fact]
    public async Task Shipment_is_created_with_the_order_number_and_weight()
    {
        carrier.Expect(HttpMethod.Post, "https://carrier.example.com/api/v2/shipments")
            .WithJsonContent<ShipmentRequest>(
                s => s.OrderNumber == "BS-2026-000042" && s.WeightKg == 1.2m, JsonSerializerOptions.Web)
            .Respond(HttpStatusCode.Created, "application/json", """{ "trackingNumber": "BS2026000042TR" }""");

        string tracking = await CreateClient().CreateShipmentAsync(
            new ShipmentRequest("BS-2026-000042", "Ada Lovelace", "Ankara", "06100", 1.2m),
            TestContext.Current.CancellationToken);

        Assert.Equal("BS2026000042TR", tracking);
        carrier.VerifyNoOutstandingExpectation();
    }
}
