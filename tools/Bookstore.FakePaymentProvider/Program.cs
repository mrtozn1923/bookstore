// A stand-in for Bookstore's external payment provider, with faults that can be changed while it runs:
//   FailRatio   share of charges answered with 503
//   SlowRatio   share of charges that take SlowDelayMs instead of a normal 40–120 ms
// GET /admin/faults shows them, PUT /admin/faults { "failRatio": 0.2 } changes them (the lab's "incident" switch).
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.UseUtcTimestamp = true;
});
builder.Services.AddSingleton(builder.Configuration.GetSection("Faults").Get<Faults>() ?? new Faults());

WebApplication app = builder.Build();

app.MapPost("/v1/charges", async (ChargeRequest charge, Faults faults, CancellationToken ct) =>
{
    if (Random.Shared.NextDouble() < faults.FailRatio)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    int delayMs = Random.Shared.NextDouble() < faults.SlowRatio ? faults.SlowDelayMs : Random.Shared.Next(40, 121);
    await Task.Delay(delayMs, ct);
    return Results.Ok(new ChargeResponse($"ch_{charge.OrderId}", "authorized"));
});

app.MapGet("/admin/faults", (Faults faults) => faults);
app.MapPut("/admin/faults", (FaultsUpdate update, Faults faults, ILogger<Program> logger) =>
{
    faults.FailRatio = update.FailRatio ?? faults.FailRatio;
    faults.SlowRatio = update.SlowRatio ?? faults.SlowRatio;
    faults.SlowDelayMs = update.SlowDelayMs ?? faults.SlowDelayMs;
    logger.LogWarning("Faults changed: failRatio={FailRatio} slowRatio={SlowRatio} slowDelayMs={SlowDelayMs}",
        faults.FailRatio, faults.SlowRatio, faults.SlowDelayMs);
    return faults;
});

app.Run();

internal sealed class Faults
{
    public double FailRatio { get; set; }
    public double SlowRatio { get; set; }
    public int SlowDelayMs { get; set; } = 1500;
}

internal sealed record FaultsUpdate(double? FailRatio, double? SlowRatio, int? SlowDelayMs);

internal sealed record ChargeRequest(int OrderId, decimal Amount, string Currency);

internal sealed record ChargeResponse(string ChargeId, string Status);
