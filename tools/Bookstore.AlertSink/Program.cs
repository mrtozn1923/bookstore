using System.Text.Json.Serialization;

// Stands in for the on-call tool, the ticket queue and the heartbeat service: Alertmanager posts its webhooks here
// (deploy/observability/alertmanager.yml) and this app writes one line per notification.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.UseUtcTimestamp = true;
});

WebApplication app = builder.Build();

app.MapPost("/alerts/{receiver}", (string receiver, AlertmanagerWebhook webhook, ILogger<Program> logger) =>
{
    foreach (Alert alert in webhook.Alerts)
    {
        logger.LogInformation("[{Receiver}] {Status} {AlertName} severity={Severity} {Summary} runbook={Runbook}",
            receiver, alert.Status, alert.Labels.GetValueOrDefault("alertname"), alert.Labels.GetValueOrDefault("severity"),
            alert.Annotations.GetValueOrDefault("summary"), alert.Annotations.GetValueOrDefault("runbook_url") ?? "-");
    }
    return Results.Ok();
});

app.Run();

// The parts of Alertmanager's webhook payload (version 4) that we print.
internal sealed record AlertmanagerWebhook(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("receiver")] string Receiver,
    [property: JsonPropertyName("alerts")] List<Alert> Alerts);

internal sealed record Alert(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("labels")] Dictionary<string, string> Labels,
    [property: JsonPropertyName("annotations")] Dictionary<string, string> Annotations);
