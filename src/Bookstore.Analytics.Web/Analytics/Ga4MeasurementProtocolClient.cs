using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Bookstore.Analytics.Web.Analytics;

public sealed class Ga4MeasurementProtocolClient(
    HttpClient http, IOptions<AnalyticsOptions> options, ILogger<Ga4MeasurementProtocolClient> logger)
{
    public async Task<bool> SendAsync(
        string clientId, string eventName, IReadOnlyDictionary<string, object> parameters, CancellationToken ct)
    {
        AnalyticsOptions o = options.Value;
        if (string.IsNullOrEmpty(o.MeasurementId) || string.IsNullOrEmpty(o.ApiSecret))
        {
            return false;
        }

        EventParameterGuard.EnsureNoPersonalData(parameters);
        string path = $"mp/collect?measurement_id={Uri.EscapeDataString(o.MeasurementId)}" +
                      $"&api_secret={Uri.EscapeDataString(o.ApiSecret)}";
        Payload payload = new(clientId, [new Event(eventName, parameters)]);

        // The endpoint answers 2xx even for a malformed event; check payloads
        // against /debug/mp/collect while developing.
        using HttpResponseMessage response = await http.PostAsJsonAsync(new Uri(o.Endpoint, path), payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Measurement Protocol returned {Status}", (int)response.StatusCode);
        }

        return response.IsSuccessStatusCode;
    }

    private sealed record Payload(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("events")] Event[] Events);

    private sealed record Event(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("params")] IReadOnlyDictionary<string, object> Params);
}
