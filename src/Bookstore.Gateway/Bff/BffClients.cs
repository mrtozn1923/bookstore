using Yarp.ReverseProxy;

namespace Bookstore.Gateway.Bff;

public static class BffClients
{
    public const string Catalog = "catalog";
    public const string Inventory = "inventory";
    public const string Ordering = "ordering";

    public static IServiceCollection AddBffClients(this IServiceCollection services)
    {
        // Catalog has three instances: pick one from the YARP cluster's available destinations.
        services.AddHttpClient(Catalog, client => client.BaseAddress = new Uri("http://catalog"))
            .AddHttpMessageHandler(sp => new ClusterDestinationHandler(sp.GetRequiredService<IProxyStateLookup>(), "catalog"));

        // Single instance services: "http://inventory" is resolved from Services:inventory:http.
        foreach (string name in (string[])[Inventory, Ordering])
        {
            services.AddHttpClient(name, client => client.BaseAddress = new Uri($"http://{name}"))
                .AddServiceDiscovery();
        }
        return services;
    }
}
