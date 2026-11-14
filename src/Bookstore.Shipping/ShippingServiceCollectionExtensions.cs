using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Shipping;

public static class ShippingServiceCollectionExtensions
{
    public static IHttpClientBuilder AddParcelTracking(this IServiceCollection services, string carrierBaseUrl) =>
        services.AddHttpClient<ParcelTrackingClient>(client =>
        {
            // Without the trailing slash, "parcels/..." would replace the last path segment ("v2")
            client.BaseAddress = new Uri(carrierBaseUrl.EndsWith('/') ? carrierBaseUrl : carrierBaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("X-Carrier-Account", "BOOKSTORE-TR");
        });
}
