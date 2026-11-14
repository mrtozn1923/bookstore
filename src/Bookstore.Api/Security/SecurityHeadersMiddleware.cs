namespace Bookstore.Api.Security;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        // the browser should not try to guess the Content-Type
        headers["X-Content-Type-Options"] = "nosniff";
        // responses must not be embedded in another site's iframe
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        // an API that returns JSON has no need to load scripts, styles or images
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        return _next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
