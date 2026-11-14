using System.Collections.Concurrent;
using System.Globalization;

namespace Bookstore.Api.Common;

// A fixed-window quota that writes the RateLimit-Policy and RateLimit fields of
// draft-ietf-httpapi-ratelimit-headers-11 on every response of the endpoints it guards.
public sealed class QuotaPolicy(string name, int quota, TimeSpan window, Func<HttpContext, string> partition, TimeProvider time)
{
    private sealed class Counter
    {
        public DateTimeOffset WindowStart;
        public int Used;
    }

    private readonly ConcurrentDictionary<string, Counter> _counters = new();

    public string Name => name;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        Counter counter = _counters.GetOrAdd(partition(http), _ => new Counter { WindowStart = time.GetUtcNow() });
        bool allowed;
        int remaining;
        int resetSeconds;
        lock (counter)
        {
            DateTimeOffset now = time.GetUtcNow();
            if (now - counter.WindowStart >= window)
            {
                counter.WindowStart = now;
                counter.Used = 0;
            }

            allowed = counter.Used < quota;
            if (allowed)
            {
                counter.Used++;
            }

            remaining = quota - counter.Used;
            resetSeconds = (int)Math.Ceiling((counter.WindowStart + window - now).TotalSeconds);
        }

        http.Response.Headers["RateLimit-Policy"] = string.Create(CultureInfo.InvariantCulture,
            $"\"{name}\";q={quota};w={(int)window.TotalSeconds}");
        http.Response.Headers["RateLimit"] = string.Create(CultureInfo.InvariantCulture,
            $"\"{name}\";r={remaining};t={resetSeconds}");
        if (!allowed)
        {
            http.Response.Headers.RetryAfter = resetSeconds.ToString(CultureInfo.InvariantCulture);
            return ProblemCatalog.Create(ProblemCatalog.QuotaExceeded, [name, quota, (int)window.TotalSeconds],
                new Dictionary<string, object?> { ["violatedPolicies"] = new[] { name } });
        }

        return await next(context);
    }
}
