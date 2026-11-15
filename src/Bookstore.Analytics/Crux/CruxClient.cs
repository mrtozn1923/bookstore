using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bookstore.Analytics.Crux;

public sealed record CruxRecord(double? LcpP75, double? InpP75, double? ClsP75, DateOnly? From, DateOnly? To);

// Chrome UX Report API: a 28-day rolling window, needs an API key (no OAuth).
public sealed class CruxClient(HttpClient http, string apiKey)
{
    private static readonly string[] Metrics =
        ["largest_contentful_paint", "interaction_to_next_paint", "cumulative_layout_shift"];

    // formFactor: PHONE, DESKTOP or TABLET
    public async Task<CruxRecord?> QueryOriginAsync(string origin, string formFactor, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync(
            $"v1/records:queryRecord?key={Uri.EscapeDataString(apiKey)}",
            new { origin, formFactor, metrics = Metrics }, ct);
        if (!response.IsSuccessStatusCode)
        {
            // e.g. an origin below the CrUX popularity threshold
            return null;
        }

        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        JsonElement record = json.RootElement.GetProperty("record");
        JsonElement metrics = record.GetProperty("metrics");
        return new CruxRecord(
            P75(metrics, "largest_contentful_paint"),
            P75(metrics, "interaction_to_next_paint"),
            P75(metrics, "cumulative_layout_shift"),
            Date(record, "firstDate"),
            Date(record, "lastDate"));
    }

    private static double? P75(JsonElement metrics, string name)
    {
        if (!metrics.TryGetProperty(name, out JsonElement metric))
        {
            return null;
        }

        JsonElement p75 = metric.GetProperty("percentiles").GetProperty("p75");
        // CLS comes as a string ("0.05"), the others as numbers
        return p75.ValueKind == JsonValueKind.String
            ? double.Parse(p75.GetString()!, CultureInfo.InvariantCulture)
            : p75.GetDouble();
    }

    private static DateOnly? Date(JsonElement record, string name) =>
        record.TryGetProperty("collectionPeriod", out JsonElement period) && period.TryGetProperty(name, out JsonElement d)
            ? new DateOnly(d.GetProperty("year").GetInt32(), d.GetProperty("month").GetInt32(), d.GetProperty("day").GetInt32())
            : null;
}
