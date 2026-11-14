using System.Diagnostics;

namespace Bookstore.Api.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Elapsed-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        await _next(context);

        _logger.LogInformation("{Method} {Path} -> {StatusCode} in {Elapsed} ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
}
