using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bookstore.Api.Health;

public class CacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly int _slowThresholdMs;

    public CacheHealthCheck(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _slowThresholdMs = configuration.GetValue("HealthChecks:CacheSlowThresholdMs", 200);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // reading a key that doesn't exist is enough: if an answer comes back, Redis is up
            await _cache.GetStringAsync("health:probe", cancellationToken).WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // use the failureStatus given at registration (Unhealthy by default)
            return new HealthCheckResult(context.Registration.FailureStatus, "Cache is unreachable.", ex);
        }

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var data = new Dictionary<string, object> { ["elapsedMs"] = elapsedMs };

        return elapsedMs >= _slowThresholdMs
            ? HealthCheckResult.Degraded("Cache is responding slowly.", data: data)
            : HealthCheckResult.Healthy("Cache is responding.", data);
    }
}
