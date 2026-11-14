using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;

namespace Bookstore.Gateway.Traffic;

public static class TrafficPolicies
{
    public const string PerUserOrders = "per-user-orders";
    public const string CatalogRead = "catalog-read";
    public const string CatalogList = "catalog-list";
    public const string BookstoreWeb = "bookstore-web";

    public static IServiceCollection AddGatewayTrafficPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PerUserOrders, context =>
            {
                // Authenticated users get their own bucket; everyone else is grouped by client IP.
                string? userId = context.User.FindFirstValue("sub");
                return userId is not null
                    ? RateLimitPartition.GetFixedWindowLimiter($"user:{userId}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                    })
                    : RateLimitPartition.GetFixedWindowLimiter($"ip:{context.Connection.RemoteIpAddress}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                    });
            });
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };
        });

        services.AddRequestTimeouts(options =>
        {
            options.AddPolicy(CatalogRead, new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(2),
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
            });
        });

        services.AddOutputCache(options =>
        {
            options.AddPolicy(CatalogList, policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("catalog"));
        });

        services.AddCors(options =>
        {
            options.AddPolicy(BookstoreWeb, policy => policy
                .WithOrigins("http://localhost:4200")
                .WithMethods("GET", "POST")
                .WithHeaders("Authorization", "Content-Type"));
        });

        return services;
    }
}
