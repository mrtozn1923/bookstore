using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace Bookstore.Gateway.LoadBalancing;

// Prefers destinations whose "Zone" metadata matches the gateway's own zone (Gateway:Zone),
// then picks the less busy of two random candidates, like PowerOfTwoChoices.
// Falls back to all available destinations when the local zone has none left.
public sealed class PreferSameZonePolicy(IConfiguration configuration) : ILoadBalancingPolicy
{
    public const string PolicyName = "PreferSameZone";
    public const string ZoneMetadataKey = "Zone";

    private readonly string? _zone = configuration["Gateway:Zone"];

    public string Name => PolicyName;

    public DestinationState? PickDestination(
        HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
    {
        if (availableDestinations.Count == 0)
        {
            return null;
        }

        List<DestinationState> local = [];
        foreach (DestinationState destination in availableDestinations)
        {
            if (destination.Model.Config.Metadata?.GetValueOrDefault(ZoneMetadataKey) == _zone)
            {
                local.Add(destination);
            }
        }

        IReadOnlyList<DestinationState> candidates = local.Count > 0 ? local : availableDestinations;
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        int first = Random.Shared.Next(candidates.Count);
        int second = Random.Shared.Next(candidates.Count - 1);
        if (second >= first)
        {
            second++;
        }

        DestinationState a = candidates[first];
        DestinationState b = candidates[second];
        return a.ConcurrentRequestCount <= b.ConcurrentRequestCount ? a : b;
    }
}
