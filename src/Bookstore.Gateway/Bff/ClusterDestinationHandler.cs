using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Model;

namespace Bookstore.Gateway.Bff;

// Sends the request to a destination that YARP currently considers available,
// so the BFF uses the same health check results as the proxy routes.
public sealed class ClusterDestinationHandler(IProxyStateLookup proxyState, string clusterId) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!proxyState.TryGetCluster(clusterId, out ClusterState? cluster))
        {
            throw new InvalidOperationException($"Cluster '{clusterId}' is not configured.");
        }

        IReadOnlyList<DestinationState> available = cluster.DestinationsState.AvailableDestinations;
        if (available.Count == 0)
        {
            throw new HttpRequestException(HttpRequestError.ConnectionError, $"Cluster '{clusterId}' has no available destination.");
        }

        DestinationState destination = available[Random.Shared.Next(available.Count)];
        request.RequestUri = new Uri(new Uri(destination.Model.Config.Address), request.RequestUri!.PathAndQuery);
        return base.SendAsync(request, cancellationToken);
    }
}
