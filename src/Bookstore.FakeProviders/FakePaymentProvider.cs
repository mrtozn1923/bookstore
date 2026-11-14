using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.FakeProviders;

// A fake card payment provider: snake_case JSON, amounts in minor units (kuruş).
public static class FakePaymentProvider
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
        // Request builders are mutable: create a new one for every mapping.
        static IRequestBuilder Charges() => Request.Create().WithPath("/v1/charges").UsingPost();

        switch (mode)
        {
            case FakeMode.Error:
                server.Given(Charges()).RespondWith(Response.Create()
                    .WithStatusCode(500)
                    .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { error = new { type = "api_error", message = "Internal error" } }));
                return;
            case FakeMode.Throttle:
                server.Given(Charges()).RespondWith(Response.Create()
                    .WithStatusCode(429)
                    .WithHeader("Retry-After", "2")
                    .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { error = new { type = "rate_limit_error", message = "Too many requests" } }));
                return;
            case FakeMode.Maintenance:
                server.Given(Charges()).RespondWith(Response.Create()
                    .WithStatusCode(503)
                    .WithHeader("Retry-After", "600")
                    .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { error = new { type = "maintenance", message = "Scheduled maintenance" } }));
                return;
        }

        server.Given(Charges().WithBody(new JsonPartialMatcher(new { source = "tok_card_declined" })))
            .AtPriority(1)
            .RespondWith(Response.Create()
                .WithStatusCode(402)
                .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { error = new { type = "card_error", code = "card_declined", message = "Your card was declined." } }));

        var approved = Response.Create()
            .WithStatusCode(201)
            .WithHeader("Content-Type", "application/json")
            .WithBody("""{"id":"ch_3MtwBwLkdIwHu7ix","object":"charge","status":"succeeded","amount":{{JsonPath.SelectToken request.body "$.amount"}},"currency":"{{JsonPath.SelectToken request.body "$.currency"}}"}""")
            .WithTransformer();

        if (mode == FakeMode.Slow)
        {
            approved = approved.WithDelay(TimeSpan.FromSeconds(5));
        }

        server.Given(Charges()).AtPriority(10).RespondWith(approved);
    }
}
