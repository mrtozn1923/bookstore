using Bookstore.Analytics.Web.Analytics;
using Bookstore.Analytics.Web.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // non-essential cookies (and our analytics) wait for consent
    options.CheckConsentNeeded = _ => true;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});
builder.Services.AddScoped<CspNonce>();
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName));
builder.Services.AddHttpClient<Ga4MeasurementProtocolClient>(http => http.Timeout = TimeSpan.FromSeconds(2));

WebApplication app = builder.Build();

app.UseContentSecurityPolicy();
app.UseStaticFiles();
app.UseCookiePolicy();
app.MapRazorPages();
app.MapAnalyticsConsent();

app.MapGet("/downloads/{name}.pdf", async (
    string name, HttpContext context, Ga4MeasurementProtocolClient ga, ILogger<Program> logger) =>
{
    if (!SeriesPdfs.TryGet(name, out byte[] pdf))
    {
        return Results.NotFound();
    }

    if (context.CanTrack() && ClientIdCookie.Read(context.Request) is { } clientId)
    {
        try
        {
            await ga.SendAsync(clientId, "pdf_download_served",
                new Dictionary<string, object> { ["file_name"] = $"{name}.pdf" }, context.RequestAborted);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // analytics must never break the download
            logger.LogWarning(ex, "Measurement Protocol call failed");
        }
    }

    return Results.File(pdf, "application/pdf", $"{name}.pdf");
});

app.Run();

public partial class Program;

internal static class SeriesPdfs
{
    private static readonly Dictionary<string, byte[]> Files = new(StringComparer.Ordinal)
    {
        ["analytics-ve-olcum"] = "%PDF-1.4\n% demo file\n"u8.ToArray(),
    };

    public static bool TryGet(string name, out byte[] pdf) => Files.TryGetValue(name, out pdf!);
}
