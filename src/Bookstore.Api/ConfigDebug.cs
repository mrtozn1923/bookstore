using Microsoft.Extensions.Configuration;

namespace Bookstore.Api;

public static class ConfigDebug
{
    // Yalnız bu önekler değeriyle gösterilir; geri kalan her şey maskelenir.
    private static readonly string[] VisiblePrefixes = ["Shipping:", "Jwt:Issuer", "Email:FromAddress"];

    public static string SafeDebugView(IConfigurationRoot root) =>
        root.GetDebugView(context =>
            VisiblePrefixes.Any(p => context.Path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                ? context.Value ?? ""
                : "***");
}
