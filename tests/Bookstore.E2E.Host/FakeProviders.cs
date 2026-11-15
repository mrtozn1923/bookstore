using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.E2E.Host;

// Ödeme ve kargo sağlayıcıları sınırda taklit edilir; her istek kabul edilir
public sealed class FakeProviders : IDisposable
{
    public WireMockServer Payments { get; } = WireMockServer.Start();

    public WireMockServer Shipping { get; } = WireMockServer.Start();

    public FakeProviders()
    {
        Payments
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBodyAsJson(new { chargeId = "ch_e2e", status = "succeeded" }));

        Shipping
            .Given(Request.Create().WithPath("/v1/shipments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201)
                .WithBodyAsJson(new { trackingNumber = "TRK-E2E-1" }));
    }

    public void Dispose()
    {
        Payments.Stop();
        Shipping.Stop();
    }
}
