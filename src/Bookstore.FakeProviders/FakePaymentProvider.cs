using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace Bookstore.FakeProviders;

public sealed class FakePaymentSettings
{
    public FakeMode Mode { get; init; } = FakeMode.Normal;

    // Slow: the charge takes this long to process. Drop: the request is lost and answered this late.
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(5);

    // Share of charge requests that fail with 500 before anything is processed.
    public double FailureRate { get; init; }

    public int Seed { get; init; } = 42;

    // When set, every successful charge is announced with a signed payment.succeeded webhook.
    public Uri? WebhookUrl { get; init; }

    public string WebhookSecret { get; init; } = FakePaymentProvider.DefaultWebhookSecret;

    // How many times the same event is delivered (providers deliver at least once).
    public int WebhookCopies { get; init; } = 1;
}

// A fake card payment provider: snake_case JSON, amounts in minor units (kuruş),
// Idempotency-Key support and a lookup endpoint for reconciliation.
public static class FakePaymentProvider
{
    // A fake example secret; Bookstore.Api's appsettings.json has the same value.
    public const string DefaultWebhookSecret = "whsec_Ym9va3N0b3JlLWZha2Utd2ViaG9vay1zZWNyZXQtMDAwMA==";

    public static WireMockServer Start(int? port = null, FakeMode mode = FakeMode.Normal) =>
        Start(port, new FakePaymentSettings { Mode = mode });

    public static WireMockServer Start(int? port, FakePaymentSettings settings)
    {
        var server = port is null ? WireMockServer.Start() : WireMockServer.Start(port.Value);
        Configure(server, settings);
        return server;
    }

    public static void Configure(WireMockServer server, FakeMode mode) =>
        Configure(server, new FakePaymentSettings { Mode = mode });

    public static void Configure(WireMockServer server, FakePaymentSettings settings)
    {
        server.Reset();
        var ledger = new Ledger(settings);

        // Request builders are mutable: create a new one for every mapping.
        server.Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithCallback(ledger.ChargeAsync));
        server.Given(Request.Create().WithPath("/v1/charges").UsingGet())
            .RespondWith(Response.Create().WithCallback(ledger.List));
    }

    private sealed class Ledger(FakePaymentSettings settings)
    {
        private readonly Random _random = new(settings.Seed);
        private readonly ConcurrentDictionary<string, KeyEntry> _keys = new();
        private readonly List<JsonObject> _charges = [];
        private int _sequence;

        public async Task<IResponseMessage> ChargeAsync(IRequestMessage request)
        {
            switch (settings.Mode)
            {
                case FakeMode.Error:
                    return Json(500, Error("api_error", null, "Internal error"));
                case FakeMode.Throttle:
                    return Json(429, Error("rate_limit_error", null, "Too many requests"), ("Retry-After", "2"));
                case FakeMode.Maintenance:
                    return Json(503, Error("maintenance", null, "Scheduled maintenance"), ("Retry-After", "600"));
                case FakeMode.Drop:
                    // The request is lost on the way: nothing is processed, the answer never arrives in time.
                    await Task.Delay(settings.Delay);
                    return Json(504, Error("api_error", null, "Gateway timeout"));
            }

            lock (_random)
            {
                if (_random.NextDouble() < settings.FailureRate)
                {
                    return Json(500, Error("api_error", null, "Internal error"));
                }
            }

            var body = request.Body ?? "";
            var key = ReadIdempotencyKey(request);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
            KeyEntry? entry = null;
            if (key is not null)
            {
                entry = new KeyEntry(fingerprint);
                var existing = _keys.GetOrAdd(key, entry);
                if (!ReferenceEquals(existing, entry))
                {
                    if (existing.Fingerprint != fingerprint)
                    {
                        return Json(422, Error("idempotency_error", "idempotency_key_reused",
                            "This Idempotency-Key was used with a different request body."));
                    }

                    if (existing.Response is not { } stored)
                    {
                        return Json(409, Error("idempotency_error", "idempotency_key_in_use",
                            "A request with this Idempotency-Key is still being processed."));
                    }

                    return Json(stored.Status, stored.Body, ("Idempotent-Replayed", "true"));
                }
            }

            if (settings.Mode == FakeMode.Slow)
            {
                await Task.Delay(settings.Delay);
            }

            var (status, json) = Process(JsonNode.Parse(body)!.AsObject());
            if (entry is not null)
            {
                entry.Response = new StoredResponse(status, json);
            }

            return Json(status, json);
        }

        public IResponseMessage List(IRequestMessage request)
        {
            var reference = request.Query?.TryGetValue("merchant_reference", out var values) == true ? values[0] : null;
            lock (_charges)
            {
                var data = new JsonArray(_charges
                    .Where(c => reference is null || (string?)c["merchant_reference"] == reference)
                    .Select(c => c.DeepClone())
                    .ToArray());
                return Json(200, new JsonObject { ["object"] = "list", ["data"] = data }.ToJsonString());
            }
        }

        private (int Status, string Body) Process(JsonObject charge)
        {
            if ((string?)charge["source"] == "tok_card_declined")
            {
                return (402, Error("card_error", "card_declined", "Your card was declined."));
            }

            var created = new JsonObject
            {
                ["id"] = $"ch_{Interlocked.Increment(ref _sequence):D6}",
                ["object"] = "charge",
                ["status"] = "succeeded",
                ["amount"] = (long?)charge["amount"],
                ["currency"] = (string?)charge["currency"],
                ["merchant_reference"] = (string?)charge["merchant_reference"]
            };
            lock (_charges)
            {
                _charges.Add(created);
            }

            if (settings.WebhookUrl is not null)
            {
                _ = FakeWebhookSender.SendAsync(settings.WebhookUrl, settings.WebhookSecret, settings.WebhookCopies, "payment.succeeded", created);
            }

            return (201, created.ToJsonString());
        }

        // The draft sends the key as a structured-field string: "value" in quotes.
        private static string? ReadIdempotencyKey(IRequestMessage request) =>
            request.Headers?.TryGetValue("Idempotency-Key", out var values) == true ? values[0].Trim('"') : null;

        private static string Error(string type, string? code, string message) =>
            JsonSerializer.Serialize(new { error = new { type, code, message } });

        private static ResponseMessage Json(int status, string body, params (string Name, string Value)[] headers)
        {
            var response = new ResponseMessage
            {
                StatusCode = status,
                BodyData = new BodyData { DetectedBodyType = BodyType.String, BodyAsString = body }
            };
            response.AddHeader("Content-Type", "application/json");
            foreach (var (name, value) in headers)
            {
                response.AddHeader(name, value);
            }

            return response;
        }
    }

    private sealed class KeyEntry(string fingerprint)
    {
        public string Fingerprint { get; } = fingerprint;

        public StoredResponse? Response { get; set; }
    }

    private sealed record StoredResponse(int Status, string Body);
}
