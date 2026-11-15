using System.Security.Cryptography;

namespace Bookstore.Admin.Security;

// A Content-Security-Policy header with a fresh nonce per response; App.razor gives the nonce to the import map.
public static class ContentSecurityPolicy
{
    public const string NonceKey = "csp-nonce";

    // The template NavMenu's inline onclick handler, allowed by its hash (see the Blazor CSP documentation).
    private const string NavMenuToggleHash = "'sha256-qnHnQs7NjQNHHNYv/I9cW+I62HzDJjbnyS/OFzqlix0='";

    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        string nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Items[NonceKey] = nonce;
        context.Response.Headers.ContentSecurityPolicy = string.Join("; ",
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "img-src 'self' data:",
            $"script-src 'self' 'wasm-unsafe-eval' 'nonce-{nonce}' 'unsafe-hashes' {NavMenuToggleHash}",
            "style-src 'self'",
            // Virtualize sizes its spacer elements with style attributes; this allows attributes, not <style> blocks.
            "style-src-attr 'unsafe-inline'",
            "connect-src 'self'");
        await next();
    });
}
