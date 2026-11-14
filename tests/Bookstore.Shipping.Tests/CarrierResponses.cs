namespace Bookstore.Shipping.Tests;

// Response bodies copied from the carrier's API documentation
internal static class CarrierResponses
{
    public const string BaseUrl = "https://carrier.example.com/api/v2";

    public const string Delivered = """
        {
          "trackingNumber": "BS2026000042TR",
          "state": "Delivered",
          "deliveredAtUtc": "2026-11-14T15:42:00Z",
          "events": [
            { "atUtc": "2026-11-13T08:10:00Z", "location": "Istanbul Hub", "description": "Picked up" },
            { "atUtc": "2026-11-14T15:42:00Z", "location": "Ankara", "description": "Delivered to recipient" }
          ]
        }
        """;
}
