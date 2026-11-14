using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Bookstore.Shipping.Tests;

public sealed class ParcelTrackingRegistrationTests
{
    private readonly MockHttpMessageHandler carrier = new();

    private ParcelTrackingClient CreateRegisteredClient(out ServiceProvider provider)
    {
        ServiceCollection services = new();
        services.AddParcelTracking(CarrierResponses.BaseUrl)
            .ConfigurePrimaryHttpMessageHandler(() => carrier);
        provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ParcelTrackingClient>();
    }

    [Fact]
    public async Task Registered_client_reads_a_delivered_parcel()
    {
        carrier.When(HttpMethod.Get, "https://carrier.example.com/api/v2/parcels/BS2026000042TR")
            .Respond("application/json", CarrierResponses.Delivered);
        ParcelTrackingClient client = CreateRegisteredClient(out ServiceProvider provider);
        using ServiceProvider _ = provider;

        ParcelStatus? status = await client.GetStatusAsync("BS2026000042TR", TestContext.Current.CancellationToken);

        Assert.Equal("Delivered", status?.State);
    }

    [Fact]
    public async Task Registered_client_sends_the_account_header_and_the_language()
    {
        carrier.Expect(HttpMethod.Get, "https://carrier.example.com/api/v2/parcels/BS2026000042TR")
            .WithQueryString("lang", "en")
            .WithHeaders("X-Carrier-Account", "BOOKSTORE-TR")
            .Respond("application/json", CarrierResponses.Delivered);
        ParcelTrackingClient client = CreateRegisteredClient(out ServiceProvider provider);
        using ServiceProvider _ = provider;

        await client.GetStatusAsync("BS2026000042TR", TestContext.Current.CancellationToken);

        carrier.VerifyNoOutstandingExpectation();
    }
}
