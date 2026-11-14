using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace Bookstore.Api;

public static partial class CacheHeaders
{
    public static void ForCatalog(HttpResponse response)
    {
        // browsers: 1 minute; any shared cache: 5 minutes
        response.Headers.CacheControl = "public, max-age=60, s-maxage=300";
        // CDNs that implement RFC 9213: 5 minutes, then stale while refreshing or while the origin is down
        response.Headers["CDN-Cache-Control"] = "max-age=300, stale-while-revalidate=60, stale-if-error=86400";
    }

    public const string PerUser = "private, no-store";

    public static void ForStaticFile(StaticFileResponseContext context) =>
        context.Context.Response.Headers.CacheControl = HashedAsset().IsMatch(context.File.Name)
            ? "public, max-age=31536000, immutable"
            : "no-cache";

    // Angular build output: main-STNEDLO4.js, chunk-GFHEENPR.js, styles-5INURTSO.css
    [GeneratedRegex(@"-[A-Z0-9]{8}\.(js|css)$")]
    private static partial Regex HashedAsset();
}
