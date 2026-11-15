using System.Buffers.Text;
using System.Security.Cryptography;

namespace Bookstore.Analytics.Web.Security;

// One random nonce per response; the inline Google tag script carries the same value.
// Base64url: Razor would HTML-encode the '+' and '=' of plain base64 inside the attribute.
public sealed class CspNonce
{
    public string Value { get; } = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
}

public static class CspMiddleware
{
    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            CspNonce nonce = context.RequestServices.GetRequiredService<CspNonce>();
            // GA4 without Ads features, as listed in Google's CSP guide
            context.Response.Headers.ContentSecurityPolicy =
                $"default-src 'self'; " +
                $"script-src-elem 'self' 'nonce-{nonce.Value}' https://www.googletagmanager.com; " +
                "img-src 'self' https://www.googletagmanager.com https://*.google-analytics.com; " +
                "connect-src 'self' https://www.googletagmanager.com https://*.google-analytics.com https://*.google.com";
            await next(context);
        });
}
