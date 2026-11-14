using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookstore.Api;

public static class EdgeProtection
{
    public const string LoginPolicy = "login";

    // Second layer behind the CDN's rate limit: per client IP, after forwarded headers are applied.
    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });

    // Azure Front Door: reject requests that did not come through our own profile.
    public static IApplicationBuilder RequireFrontDoorId(this IApplicationBuilder app, string? frontDoorId) =>
        frontDoorId is null
            ? app
            : app.Use(async (context, next) =>
            {
                if (context.Request.Headers["X-Azure-FDID"] != frontDoorId)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                await next();
            });
}
