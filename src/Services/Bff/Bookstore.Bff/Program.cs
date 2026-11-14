using System.Threading.RateLimiting;
using Bookstore.Bff.Books;
using Bookstore.Inventory.V1;
using Yarp.ReverseProxy.Transforms;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

// Proxy routes from configuration; "https+http://catalog" addresses are resolved by Aspire's service discovery.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(context =>
    {
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
builder.Services.AddGrpcClient<InventoryService.InventoryServiceClient>(options => options.Address = new Uri("http://_grpc.inventory"));

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
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapBookPage();
app.MapReverseProxy();

// The Angular application (post 11) is served from wwwroot; unknown paths fall back to index.html.
app.UseDefaultFiles();
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();
