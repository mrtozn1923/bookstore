using System.ComponentModel.DataAnnotations;

namespace Bookstore.Payments;

// Fault injection for game days, bound to the "Chaos" section (normally set in switches.json, so a game day can be
// started and aborted without a restart). The strategies are not even registered in Production.
public sealed class ChaosOptions
{
    public const string SectionName = "Chaos";

    public bool Enabled { get; set; }

    // Share of payment calls answered with 503 by the chaos strategy instead of the provider.
    [Range(0.0, 1.0)]
    public double FaultRate { get; set; }

    // Share of payment calls delayed by LatencyMs before they are sent.
    [Range(0.0, 1.0)]
    public double LatencyRate { get; set; }

    [Range(0, 30_000)]
    public int LatencyMs { get; set; } = 1000;
}
