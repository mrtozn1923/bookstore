namespace Bookstore.Api.Integrations;

public sealed class ShippingOptions
{
    public Uri BaseUrl { get; set; } = new("https://shipping.invalid/");
}

public sealed record ShipmentRequest(string OrderReference, string City, int ItemCount);

public sealed record ShipmentResponse(string TrackingNumber);

public sealed class ShippingClient(HttpClient httpClient)
{
    public async Task<string> CreateShipmentAsync(ShipmentRequest request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("v1/shipments", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        ShipmentResponse? body = await response.Content.ReadFromJsonAsync<ShipmentResponse>(cancellationToken);
        return body!.TrackingNumber;
    }
}
