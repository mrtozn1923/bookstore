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
            // The service discovery destination resolver runs before config filters and renames
            // "catalog-1" to "catalog-1[localhost:5301]"; look the override up by the configured id.
            string configuredId = id.Split('[')[0];
            string? address = configuration[$"DestinationAddresses:{configuredId}"];
            destinations[id] = string.IsNullOrEmpty(address)
                ? destination
                : destination with { Address = address };
        }

        return new(cluster with { Destinations = destinations });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(
        RouteConfig route, ClusterConfig? cluster, CancellationToken cancel) => new(route);
}
