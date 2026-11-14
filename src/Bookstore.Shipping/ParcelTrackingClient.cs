using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bookstore.Shipping;

// Typed client for the carrier's parcel tracking API
public sealed class ParcelTrackingClient(HttpClient http)
{
    public async Task<ParcelStatus?> GetStatusAsync(string trackingNumber, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"parcels/{Uri.EscapeDataString(trackingNumber)}?lang=en");
        using HttpResponseMessage response = await SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<ParcelStatus>(response, ct);
    }

    public async Task<string> CreateShipmentAsync(ShipmentRequest shipment, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "shipments") { Content = JsonContent.Create(shipment) };
        using HttpResponseMessage response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        ShipmentCreated created = await ReadAsync<ShipmentCreated>(response, ct);
        return created.TrackingNumber;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new CarrierException($"Carrier did not respond within {http.Timeout.TotalSeconds}s.", innerException: ex);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
        {
            HttpStatusCode status = response.StatusCode;
            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
            response.Dispose();
            throw new CarrierException($"Carrier returned {(int)status} {status}.", status, retryAfter);
        }

        return response;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(ct)
                ?? throw new CarrierException("Carrier sent an empty body.", response.StatusCode);
        }
        catch (JsonException ex)
        {
            throw new CarrierException("Carrier sent a body we cannot read.", response.StatusCode, innerException: ex);
        }
    }
}
