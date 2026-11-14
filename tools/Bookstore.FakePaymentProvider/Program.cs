using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

// A stand-in for the external payment provider. Four nodes sit behind its load balancer and one of them is slow:
// every attempt has a SlowRatio chance of taking SlowDelayMs instead of a normal 40–120 ms.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss.fff ";
    options.UseUtcTimestamp = true;
});

WebApplication app = builder.Build();

double slowRatio = app.Configuration.GetValue("Provider:SlowRatio", 0.25);
int slowDelayMs = app.Configuration.GetValue("Provider:SlowDelayMs", 3000);
ConcurrentDictionary<string, int> attempts = new();

app.MapPost("/v1/charges", async (
    ChargeRequest charge, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
    ILogger<Program> logger, CancellationToken ct) =>
{
    int attempt = attempts.AddOrUpdate(idempotencyKey, 1, (_, n) => n + 1);
    bool slow = Random.Shared.NextDouble() < slowRatio;
    int delayMs = slow ? slowDelayMs : Random.Shared.Next(40, 121);
    try
    {
        await Task.Delay(delayMs, ct);
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("{Key} attempt {Attempt}: client gave up after waiting", idempotencyKey, attempt);
        return Results.Empty;
    }

    logger.LogInformation("{Key} attempt {Attempt}: authorized {Amount} {Currency} in {DelayMs} ms",
        idempotencyKey, attempt, charge.Amount, charge.Currency, delayMs);
    return Results.Ok(new ChargeResponse($"ch_{charge.OrderId}", "authorized"));
});

app.Run();

internal sealed record ChargeRequest(int OrderId, decimal Amount, string Currency);

internal sealed record ChargeResponse(string ChargeId, string Status);
