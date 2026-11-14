using Yarp.ReverseProxy.Configuration;

namespace Bookstore.Gateway.Configuration;

// Lets each environment override a destination address without touching the route table:
// DestinationAddresses__catalog-1=http://catalog:8080/
public sealed class DestinationAddressFilter(IConfiguration configuration) : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        if (cluster.Destinations is null)
        {
            return new(cluster);
        }

        Dictionary<string, DestinationConfig> destinations = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string id, DestinationConfig destination) in cluster.Destinations)
        {
            string? address = configuration[$"DestinationAddresses:{id}"];
            destinations[id] = string.IsNullOrEmpty(address)
                ? destination
                : destination with { Address = address };
        }

        return new(cluster with { Destinations = destinations });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(
        RouteConfig route, ClusterConfig? cluster, CancellationToken cancel) => new(route);
}
