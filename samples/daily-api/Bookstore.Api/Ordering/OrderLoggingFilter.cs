using System.Diagnostics;

namespace Bookstore.Api.Ordering;

public class OrderLoggingFilter(ILogger<OrderLoggingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        long start = Stopwatch.GetTimestamp();
        string endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
        logger.LogInformation("[group] before {Endpoint}", endpoint);

        object? result = await next(context);

        // Sonuç henüz yazılmadı; elimizde yalnız IResult object'i var
        logger.LogInformation("[group] after {Endpoint} -> {Result} in {Elapsed:F1} ms",
            endpoint, result?.GetType().Name, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        return result;
    }
}
