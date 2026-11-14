using System.Net;
using Bookstore.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

namespace Bookstore.Hosting;

public static class CloudHostingExtensions
{
    // App Service and Container Apps terminate TLS in front of the app and add X-Forwarded-* headers.
    // Trust them only from the networks listed in Proxy:KnownNetworks (semicolon-separated CIDR ranges).
    public static IServiceCollection AddCloudHosting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            foreach (string cidr in (configuration["Proxy:KnownNetworks"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr.Trim()));
            }
        });

        services.AddHealthChecks()
            .AddDbContextCheck<BookstoreContext>("database", tags: ["ready"]);
        return services;
    }

    // /health/live: the process answers. /health: the API can also reach BookstoreDb.
    public static WebApplication MapCloudHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        return app;
    }
}
