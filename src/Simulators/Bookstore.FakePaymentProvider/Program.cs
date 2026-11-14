using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// A minimal card payment provider: charges with an Idempotency-Key, asynchronous results delivered as
// signed webhooks (Standard Webhooks format), lookups by merchant reference and refunds.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("webhooks");
builder.Services.AddSingleton<ProviderState>();
// The provider's API speaks snake_case JSON, like many real payment APIs.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

WebApplication app = builder.Build();

app.MapPost("/v1/charges", async (ChargeRequest request, HttpRequest http, ProviderState state, IHttpClientFactory clients,
    IConfiguration configuration, ILogger<ProviderState> logger) =>
{
    if (http.Headers["Idempotency-Key"].ToString() is not { Length: > 0 } key)
    {
        return Results.Problem(title: "Idempotency-Key header is required", statusCode: 400);
    }

    // The same key returns the same charge; it is never charged twice.
    if (state.ChargesByKey.TryGetValue(key, out Charge? existing))
    {
        http.HttpContext.Response.Headers["Idempotent-Replayed"] = "true";
        return Results.Ok(existing.ToResponse());
    }

    Charge charge = new($"ch_{Guid.NewGuid():N}", request.Reference, request.Amount, request.Currency);
    if (!state.ChargesByKey.TryAdd(key, charge))
    {
        return Results.Ok(state.ChargesByKey[key].ToResponse());
    }
    state.ChargesById[charge.Id] = charge;
    logger.LogInformation("Charge {ChargeId} for {Reference}: {Amount} {Currency} accepted (key {Key})",
        charge.Id, charge.Reference, charge.Amount, charge.Currency, key);

    // The result comes later, as a webhook: card networks answer asynchronously.
    _ = Task.Run(() => SettleAsync(charge, state, clients, configuration, logger));

    // Simulated slowness: the charge is created, but the caller may give up before it sees the answer.
    if (state.ResponseDelay > TimeSpan.Zero)
    {
        await Task.Delay(state.ResponseDelay);
    }
    return Results.Accepted($"/v1/charges/{charge.Id}", charge.ToResponse());
});

app.MapGet("/v1/charges", (string reference, ProviderState state) =>
    state.ChargesById.Values.FirstOrDefault(c => c.Reference == reference) is { } charge
        ? Results.Ok(charge.ToResponse())
        : Results.NotFound());

app.MapPost("/v1/refunds", (RefundRequest request, HttpRequest http, ProviderState state, ILogger<ProviderState> logger) =>
{
    if (!state.ChargesById.TryGetValue(request.ChargeId, out Charge? charge) || charge.Status != "succeeded")
    {
        return Results.Problem(title: "Only a succeeded charge can be refunded", statusCode: 409);
    }
    string key = http.Headers["Idempotency-Key"].ToString();
    string refundId = state.RefundsByKey.GetOrAdd(key, _ => $"re_{Guid.NewGuid():N}");
    logger.LogInformation("Refund {RefundId} for charge {ChargeId} (key {Key})", refundId, charge.Id, key);
    return Results.Ok(new { id = refundId, charge_id = charge.Id, status = "succeeded" });
});

// Test hooks, not part of the "provider API": make responses slow to produce timeouts.
app.MapPost("/simulate/response-delay/{milliseconds:int}", (int milliseconds, ProviderState state) =>
{
    state.ResponseDelay = TimeSpan.FromMilliseconds(milliseconds);
    return Results.NoContent();
});

// Lost webhooks: the charge is settled, but Payments is never told.
app.MapPost("/simulate/drop-webhooks/{drop:bool}", (bool drop, ProviderState state) =>
{
    state.DropWebhooks = drop;
    return Results.NoContent();
});

app.Run();

static async Task SettleAsync(Charge charge, ProviderState state, IHttpClientFactory clients, IConfiguration configuration, ILogger logger)
{
    await Task.Delay(configuration.GetValue("Provider:ProcessingDelay", TimeSpan.FromSeconds(1)));
    bool declined = charge.Amount > configuration.GetValue("Provider:DeclineAbove", 5000m);
    charge.Status = declined ? "failed" : "succeeded";
    charge.FailureCode = declined ? "card_declined" : null;

    string body = JsonSerializer.Serialize(new
    {
        type = declined ? "charge.failed" : "charge.succeeded",
        data = charge.ToResponse()
    });
    string eventId = $"evt_{Guid.NewGuid():N}";
    string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    // Standard Webhooks: HMAC-SHA256 over "id.timestamp.body", base64, with the "v1," prefix.
    byte[] key = Convert.FromBase64String(configuration["Webhooks:Secret"]!["whsec_".Length..]);
    string signature = Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{eventId}.{timestamp}.{body}")));

    if (state.DropWebhooks)
    {
        logger.LogWarning("Webhook {EventId} for {ChargeId} dropped (simulation)", eventId, charge.Id);
        return;
    }

    for (int attempt = 1; attempt <= 5; attempt++)
    {
        try
        {
            using HttpRequestMessage message = new(HttpMethod.Post, configuration["Webhooks:Url"])
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            message.Headers.Add("webhook-id", eventId);
            message.Headers.Add("webhook-timestamp", timestamp);
            message.Headers.Add("webhook-signature", $"v1,{signature}");
            using HttpResponseMessage response = await clients.CreateClient("webhooks").SendAsync(message);
            logger.LogInformation("Webhook {EventId} {Type} for {ChargeId}: attempt {Attempt} -> {Status}",
                eventId, declined ? "charge.failed" : "charge.succeeded", charge.Id, attempt, (int)response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Webhook {EventId} attempt {Attempt} failed: {Error}", eventId, attempt, ex.Message);
        }
        await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
    }
}

sealed class ProviderState
{
    public ConcurrentDictionary<string, Charge> ChargesByKey { get; } = new();
    public ConcurrentDictionary<string, Charge> ChargesById { get; } = new();
    public ConcurrentDictionary<string, string> RefundsByKey { get; } = new();
    public TimeSpan ResponseDelay { get; set; }
    public bool DropWebhooks { get; set; }
}

sealed record ChargeRequest(decimal Amount, string Currency, string Reference);

sealed record RefundRequest(string ChargeId);

sealed class Charge(string id, string reference, decimal amount, string currency)
{
    public string Id { get; } = id;
    public string Reference { get; } = reference;
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
    public string Status { get; set; } = "pending";
    public string? FailureCode { get; set; }

    // The provider's own wire format (snake_case); Payments translates it at its boundary.
    public object ToResponse() => new
    {
        id = Id, reference = Reference, amount = Amount, currency = Currency, status = Status, failure_code = FailureCode
    };
}
