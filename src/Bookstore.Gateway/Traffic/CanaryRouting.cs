using System.Globalization;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Model;

namespace Bookstore.Gateway.Traffic;

public static class CanaryRouting
{
    public const string ClusterKey = "Canary.ClusterId";
    public const string PercentKey = "Canary.Percent";
    public const string BucketCookie = "bookstore-bucket";

    // Route metadata decides: Canary.ClusterId is the new target, Canary.Percent (0-100) its share.
    // A visitor keeps the same bucket through a cookie, so a page reload doesn't switch versions.
    public static IReverseProxyApplicationBuilder UseCanary(this IReverseProxyApplicationBuilder proxyPipeline)
    {
        proxyPipeline.Use((context, next) =>
        {
            IReadOnlyDictionary<string, string>? metadata = context.GetReverseProxyFeature().Route.Config.Metadata;
            if (metadata is not null
                && metadata.TryGetValue(ClusterKey, out string? canaryClusterId)
                && metadata.TryGetValue(PercentKey, out string? percentText)
                && int.TryParse(percentText, CultureInfo.InvariantCulture, out int percent)
                && GetOrAssignBucket(context) < percent
                && context.RequestServices.GetRequiredService<IProxyStateLookup>().TryGetCluster(canaryClusterId, out ClusterState? canary))
            {
                context.ReassignProxyRequest(canary);
            }
            return next();
        });
        return proxyPipeline;
    }

    private static int GetOrAssignBucket(HttpContext context)
    {
        if (int.TryParse(context.Request.Cookies[BucketCookie], CultureInfo.InvariantCulture, out int bucket) && bucket is >= 0 and < 100)
        {
            return bucket;
        }

        bucket = Random.Shared.Next(100);
        context.Response.Cookies.Append(BucketCookie, bucket.ToString(CultureInfo.InvariantCulture), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
        });
        return bucket;
    }
}
