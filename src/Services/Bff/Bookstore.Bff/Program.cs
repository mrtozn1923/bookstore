using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Bookstore.Bff.Books;
using Bookstore.Bff.Security;
using Microsoft.AspNetCore.Authentication;
using Bookstore.Inventory.V1;
using Yarp.ReverseProxy.Transforms;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment.IsProduction());

// Proxy routes from configuration; "https+http://catalog" addresses are resolved by Aspire's service discovery.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(context =>
    {
        // Token propagation: the signed-in user's access token goes to the service as a Bearer token.
        context.AddRequestTransform(async transform =>
        {
            if (transform.HttpContext.User.Identity?.IsAuthenticated == true &&
                await transform.HttpContext.GetTokenAsync("access_token") is { } accessToken)
            {
                transform.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
            }
        });

        // Ordering answers 202 with Location: http://ordering-host/api/v1/orders/{id}. The browser must
        // see the BFF's public path, never an internal address.
        if (context.Route.ClusterId == "ordering")
        {
            context.AddResponseTransform(transform =>
            {
                if (transform.HttpContext.Response.Headers.Location.ToString() is { Length: > 0 } location &&
                    Uri.TryCreate(location, UriKind.Absolute, out Uri? internalUri))
                {
                    transform.HttpContext.Response.Headers.Location = internalUri.AbsolutePath.Replace("/api/v1/orders", "/api/orders");
                }
                return ValueTask.CompletedTask;
            });
        }
    });

// Clients for the aggregation endpoint.
builder.Services.AddHttpClient<CatalogApi>(client => client.BaseAddress = new Uri("https+http://catalog"));
builder.Services.AddHttpClient<ServiceTokenProvider>(client => client.BaseAddress = new Uri("https+http://keycloak"));
builder.Services.AddGrpcClient<InventoryService.InventoryServiceClient>(options => options.Address = new Uri("http://_grpc.inventory"))
    // The stock query is the BFF's own call, not the user's: it carries the BFF's client credentials token.
    .AddCallCredentials(async (_, metadata, services) =>
        metadata.Add("Authorization", $"Bearer {await services.GetRequiredService<ServiceTokenProvider>().GetAsync(default)}"))
    // gRPC refuses to send call credentials over a channel without TLS. Locally the channel is plain HTTP/2;
    // in production TLS (or the service mesh) protects it and this setting goes away.
    .ConfigureChannel(channel => channel.UnsafeUseInsecureChannelCallCredentials = !builder.Environment.IsProduction());

// Placing an order is the expensive route: at most 5 per 10 seconds per client address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("place-order", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromSeconds(10), QueueLimit = 0 }));
});

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
// The Angular build output (web/bookstore-web, post 11) lands in wwwroot. In a production build the file names
// carry a content hash (main-UPE27H4M.js), so those files can be cached for a year; index.html must never be,
// it points at the current hashes. UseStaticFiles instead of MapStaticAssets: during development
// "ng build --watch" writes new files after the BFF was built, and MapStaticAssets only knows the files
// that existed at build time.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name == "index.html")
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }
        else if (HashedFileName().IsMatch(context.File.Name))
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgeryHeader();
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapBffEndpoints();
app.MapBookPage();
app.MapReverseProxy();

// Client-side routes (/books/..., /orders/...) all get index.html; Angular's router takes over.
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
});

app.Run();

internal static partial class Program
{
    // "main-UPE27H4M.js", "styles-N2GZJMYJ.css": the hash Angular's production build adds
    [GeneratedRegex(@"-[A-Z0-9]{8}\.(js|css)$")]
    private static partial Regex HashedFileName();
}
