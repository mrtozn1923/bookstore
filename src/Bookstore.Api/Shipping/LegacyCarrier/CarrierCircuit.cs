using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;

namespace Bookstore.Shipping.LegacyCarrier;

// One breaker per process for the carrier: we can read its state and open it by hand.
public sealed class CarrierCircuit
{
    public CircuitBreakerManualControl ManualControl { get; } = new();

    public CircuitBreakerStateProvider StateProvider { get; } = new();
}

public sealed class CarrierCircuitHealthCheck(CarrierCircuit circuit) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var state = circuit.StateProvider.CircuitState;
        return Task.FromResult(state == CircuitState.Closed
            ? HealthCheckResult.Healthy("Carrier circuit is closed.")
            : HealthCheckResult.Degraded($"Carrier circuit is {state}; shipping uses the flat tariff."));
    }
}
