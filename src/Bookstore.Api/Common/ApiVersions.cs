using System.Globalization;

namespace Bookstore.Api.Common;

// A dated version of the contract (ADR-0004). Only breaking changes get a new version.
public sealed record ApiVersion(string Name, DateTimeOffset? DeprecatedAt = null, DateTimeOffset? SunsetAt = null)
{
    public string DocumentationUrl => $"https://api.example.com/docs/versions/{Name}";
}

public static class ApiVersions
{
    public const string QueryName = "api-version";

    // 2026-11-15 removed Book.author (use authors).
    public static readonly ApiVersion V20260901 = new("2026-09-01",
        DeprecatedAt: new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.Zero),
        SunsetAt: new DateTimeOffset(2027, 5, 17, 0, 0, 0, TimeSpan.Zero));
    public static readonly ApiVersion V20261115 = new("2026-11-15");

    public static readonly IReadOnlyList<ApiVersion> Supported = [V20260901, V20261115];

    // Clients that never sent a version keep the oldest supported one until its sunset.
    public static readonly ApiVersion Default = V20260901;

    public static ApiVersion Current(this HttpContext http) =>
        http.Items[typeof(ApiVersion)] as ApiVersion ?? Default;

    // RFC 9745 Deprecation (structured field date), RFC 8594 Sunset (HTTP-date) and the deprecation link.
    public static void WriteDeprecation(HttpResponse response, DateTimeOffset deprecatedAt, DateTimeOffset? sunsetAt,
        string documentationUrl)
    {
        response.Headers["Deprecation"] = "@" + deprecatedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        if (sunsetAt is { } sunset)
        {
            response.Headers["Sunset"] = sunset.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
        }

        response.Headers.Append("Link", $"<{documentationUrl}>; rel=\"deprecation\"; type=\"text/html\"");
    }

    public static IApplicationBuilder UseApiVersions(this IApplicationBuilder app) => app.Use(async (http, next) =>
    {
        // /legacy/rpc predates versioning and has its own deprecation.
        if (http.Request.Path.StartsWithSegments("/legacy"))
        {
            await next(http);
            return;
        }

        string[] requested = http.Request.Query[QueryName].ToArray()!;
        ApiVersion? version = requested.Length switch
        {
            0 => Default,
            1 => Supported.FirstOrDefault(v => v.Name == requested[0]),
            _ => null,
        };
        if (version is null)
        {
            await ProblemCatalog.Create(ProblemCatalog.UnsupportedApiVersion, [string.Join(',', requested)],
                    new Dictionary<string, object?> { ["supportedVersions"] = Supported.Select(v => v.Name).ToArray() })
                .ExecuteAsync(http);
            return;
        }

        http.Items[typeof(ApiVersion)] = version;
        http.Response.Headers["Api-Version"] = version.Name;
        if (version.DeprecatedAt is { } deprecatedAt)
        {
            WriteDeprecation(http.Response, deprecatedAt, version.SunsetAt, version.DocumentationUrl);
            // Usage telemetry: who still calls a deprecated version.
            http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Bookstore.Deprecation")
                .LogInformation("Deprecated api-version {Version} used by {Client} for {Method} {Path}",
                    version.Name, http.User.Identity?.Name ?? "anonymous", http.Request.Method, http.Request.Path.Value);
        }

        await next(http);
    });
}
