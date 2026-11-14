using WireMock.Matchers;
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
        ConfigureAuth(server);
        var quotes = Request.Create().WithPath("/api/v2/quotes").UsingGet()
            .WithHeader("Authorization", new WildcardMatcher("Bearer fake-carrier-token-*"));

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

        server.Given(Request.Create().WithPath("/api/v2/shipments/*/events").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new[]
                {
                    new { CODE = "01", AT = "2026-11-12T09:14:00Z", PLACE = "Istanbul hub" },
                    new { CODE = "03", AT = "2026-11-12T21:40:00Z", PLACE = "Ankara hub" },
                    new { CODE = "05", AT = "2026-11-13T08:05:00Z", PLACE = "Cankaya branch" }
                }));
    }

    // OAuth 2.0 client credentials: every token is valid for 120 seconds.
    private static void ConfigureAuth(WireMockServer server)
    {
        var issued = 0;
        server.Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create().WithCallback(_ => new WireMock.ResponseMessage
            {
                StatusCode = 200,
                Headers = new Dictionary<string, WireMock.Types.WireMockList<string>>
                {
                    ["Content-Type"] = new("application/json")
                },
                BodyData = new WireMock.Util.BodyData
                {
                    DetectedBodyType = WireMock.Types.BodyType.String,
                    BodyAsString = $$"""{"access_token":"fake-carrier-token-{{Interlocked.Increment(ref issued)}}","token_type":"Bearer","expires_in":120}"""
                }
            }));

        server.Given(Request.Create().WithPath("/api/*").UsingAnyMethod())
            .AtPriority(100)
            .RespondWith(Response.Create().WithStatusCode(401).WithHeader("WWW-Authenticate", "Bearer"));
    }
}
