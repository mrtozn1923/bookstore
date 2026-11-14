using Microsoft.AspNetCore.HttpOverrides;

namespace Bookstore.Api;

public static class CloudflareForwarding
{
    // Trust CF-Connecting-IP and X-Forwarded-Proto only when the connection itself comes from Cloudflare.
    public static IServiceCollection AddCloudflareForwarding(this IServiceCollection services, string rangesFile)
    {
        var ranges = File.ReadAllLines(rangesFile)
            .Where(line => line.Length > 0)
            .Select(System.Net.IPNetwork.Parse)
            .ToList();

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardedForHeaderName = "CF-Connecting-IP";
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            ranges.ForEach(options.KnownIPNetworks.Add);
        });
    }
}
