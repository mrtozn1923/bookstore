using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.FakeProviders;

// A fake legacy cargo carrier: UPPER_CASE fields, prices as strings.
public static class FakeLegacyCarrier
{
    public static WireMockServer Start(int? port = null, FakeMode mode = FakeMode.Normal)
    {
        var server = port is null ? WireMockServer.Start() : WireMockServer.Start(port.Value);
        Configure(server, mode);
        return server;
    }

    public static void Configure(WireMockServer server, FakeMode mode)
    {
        server.Reset();
        var quotes = Request.Create().WithPath("/api/v2/quotes").UsingGet();

        var response = mode switch
        {
            FakeMode.Error => Response.Create().WithStatusCode(500).WithBody("Internal Server Error"),
            FakeMode.Throttle => Response.Create().WithStatusCode(429).WithHeader("Retry-After", "2"),
            FakeMode.Maintenance => Response.Create().WithStatusCode(503).WithHeader("Retry-After", "600")
                .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { ERR_CODE = "MAINTENANCE", ERR_MSG = "Service unavailable until 03:00 UTC" }),
            FakeMode.SchemaChanged => Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { QUOTE_ID = "Q-77812", PRICE = new { AMOUNT = 89.90m, CUR = "TRY" }, SVC = "STD", ETA_DAYS = 2 }),
            _ => Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { QUOTE_ID = "Q-77812", AMT = "89.90", CUR = "TRY", SVC = "STD", ETA_DAYS = 2 })
        };

        if (mode == FakeMode.Slow)
        {
            response = response.WithDelay(TimeSpan.FromSeconds(5));
        }

        server.Given(quotes).RespondWith(response);
    }
}
