namespace Bookstore.Infrastructure;

// Fault injection settings, read on every call (IOptionsMonitor) so they can be changed without a restart.
public sealed class ChaosOptions
{
    public bool Enabled { get; set; }
    public double FaultRate { get; set; }
    public double LatencyRate { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.FromSeconds(3);
}
