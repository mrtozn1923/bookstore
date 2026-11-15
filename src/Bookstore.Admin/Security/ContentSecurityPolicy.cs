using System.Security.Cryptography;

namespace Bookstore.Admin.Security;

// A Content-Security-Policy header with a fresh nonce per response; App.razor gives the nonce to the import map.
public static class ContentSecurityPolicy
{
    public const string NonceKey = "csp-nonce";

    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        string nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Items[NonceKey] = nonce;
        context.Response.Headers.ContentSecurityPolicy = string.Join("; ",
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "img-src 'self' data:",
            $"script-src 'self' 'wasm-unsafe-eval' 'nonce-{nonce}'",
            "style-src 'self'",
            // Virtualize sizes its spacer elements with style attributes; this allows attributes, not <style> blocks.
            "style-src-attr 'unsafe-inline'",
            "connect-src 'self'");
        await next();
    });
}
