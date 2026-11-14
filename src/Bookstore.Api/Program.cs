using Bookstore.Api;
using OpenTelemetry.Trace;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// One multiplexer for the whole application, shared by every request
string redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is missing.");
IConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);
builder.Services.AddSingleton(redis);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<PriceInvalidationListener>();

// Post 14: a span per Redis command under the ASP.NET Core request span; console output only when asked for
builder.Services.AddOpenTelemetry().WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation().AddRedisInstrumentation();
    if (builder.Configuration.GetValue<bool>("Telemetry:Console"))
    {
        tracing.AddConsoleExporter();
    }
});

WebApplication app = builder.Build();

await CatalogEndpoints.EnsureIndexAsync(redis.GetDatabase());
if (!await InventoryEndpoints.FunctionsLoadedAsync(redis.GetDatabase()))
{
    app.Logger.LogWarning("Redis Functions library 'bookstore' is not loaded: stock reservations will fail");
}

app.MapBookViews();
app.MapVerification();
app.MapOrders();
app.MapCart();
app.MapRecentlyViewed();
app.MapTags();
app.MapVisitors();
app.MapBestsellers();
app.MapWarehouses();
app.MapInventory();
app.MapBookPrices();
app.MapCatalog();

app.Run();
