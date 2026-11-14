using System.Diagnostics;
using System.Globalization;

namespace Bookstore.Api.Middleware;

public class ResponseTimeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        long start = Stopwatch.GetTimestamp();

        // Header'lar gönderilmeden hemen önce çalışır
        context.Response.OnStarting(() =>
        {
            TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
            context.Response.Headers["X-Response-Time-Ms"] = elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });

        await next(context);
    }
}
