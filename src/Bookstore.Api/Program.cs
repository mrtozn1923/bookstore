using Bookstore.Api;
using Bookstore.Api.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Yalnız BOOKSTORE_ ile başlayan değişkenler, önek atılarak
builder.Configuration.AddEnvironmentVariables(prefix: "BOOKSTORE_");

builder.Services.AddBookstoreOptions();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Hangi değer hangi provider'dan geldi; secret'lar maskeli
    app.MapGet("/debug/config", (IConfiguration config) =>
        ConfigDebug.SafeDebugView((IConfigurationRoot)config));
}

// Reports which settings are present, never their values.
app.MapGet("/api/info", (IHostEnvironment env, IConfiguration config, IOptionsMonitor<ShippingOptions> shipping) => new
{
    Environment = env.EnvironmentName,
    shipping.CurrentValue.FreeShippingThreshold,
    Secrets = new Dictionary<string, bool>
    {
        ["ConnectionStrings:Bookstore"] = !string.IsNullOrEmpty(config.GetConnectionString("Bookstore")),
        ["Jwt:SigningKey"] = !string.IsNullOrEmpty(config["Jwt:SigningKey"]),
        ["Email:ApiKey"] = !string.IsNullOrEmpty(config["Email:ApiKey"]),
        ["Payments:Webhook:Secret"] = !string.IsNullOrEmpty(config["Payments:Webhook:Secret"])
    }
});

// Üç options interface'inin aynı anda ne gördüğü
app.MapGet("/api/shipping/threshold", (
    IOptions<ShippingOptions> options,
    IOptionsSnapshot<ShippingOptions> snapshot,
    IOptionsMonitor<ShippingOptions> monitor) => new
{
    Options = options.Value.FreeShippingThreshold,
    Snapshot = snapshot.Value.FreeShippingThreshold,
    Monitor = monitor.CurrentValue.FreeShippingThreshold
});

app.Run();
