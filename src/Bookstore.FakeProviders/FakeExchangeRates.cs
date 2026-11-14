using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.FakeProviders;

// A fake exchange-rate service; Bookstore runs two instances (primary and secondary region).
public static class FakeExchangeRates
{
    public static WireMockServer Start(int? port, string source, TimeSpan delay)
    {
        var server = port is null ? WireMockServer.Start() : WireMockServer.Start(port.Value);
        server.Given(Request.Create().WithPath("/api/rates/EUR-TRY").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { pair = "EUR-TRY", rate = 44.1250m, source })
                .WithDelay(delay));
        return server;
    }
}
