namespace Bookstore.Catalog;

public sealed class InstanceInfo
{
    public const string Header = "X-Catalog-Instance";

    public required string Name { get; init; }

    // Artificial delay in milliseconds to simulate a slow instance in load balancing demos.
    public int ResponseDelayMs { get; init; }

    // Development only: lets a demo flip the health endpoint to Unhealthy while the process keeps running.
    public bool ForceUnhealthy { get; set; }
}
