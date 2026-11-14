using System.Text.Json;

namespace Bookstore.Api.Rum;

// One metric report as sent by the Angular client (web-vitals attribution build)
public sealed record RumReport(
    string Name,
    double Value,
    string Rating,
    string Route,
    string Device,
    string NavigationType,
    Dictionary<string, JsonElement>? Attribution);

public sealed record RumSample(RumReport Report, DateTimeOffset ReceivedAt);

public sealed record RumRow(
    string Route,
    string Metric,
    string Device,
    int Samples,
    double P75,
    string Rating,
    bool Alarm);
