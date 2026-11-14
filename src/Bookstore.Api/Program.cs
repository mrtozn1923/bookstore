using Azure.Security.KeyVault.Secrets;
using Bookstore.Api;
using Bookstore.Api.AppConfiguration;
using Bookstore.Api.Cloud;
using Bookstore.Api.KeyVault;
using Bookstore.Api.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Yalnız BOOKSTORE_ ile başlayan değişkenler, önek atılarak
builder.Configuration.AddEnvironmentVariables(prefix: "BOOKSTORE_");

var credential = AzureCredentials.Create(builder.Environment, builder.Configuration);

// Gizli olmayan, merkezde yönetilen ayarlar: App Configuration
var useAppConfiguration = AppConfigurationExtensions.IsEnabled(builder.Configuration);
if (useAppConfiguration)
{
    builder.Configuration.AddBookstoreAppConfiguration(builder.Configuration, builder.Environment, credential);
    builder.Services.AddAzureAppConfiguration();
}

// Vault adresi environment'tan gelir: her environment'ın kendi vault'u var
if (builder.Configuration["KeyVault:Uri"] is { Length: > 0 } vaultUri)
{
    var clientOptions = new SecretClientOptions
    {
        // Yalnız yerel emulator için; Production'da bu kontrol asla kapatılmaz
        DisableChallengeResourceVerification = !builder.Environment.IsProduction()
            && builder.Configuration.GetValue<bool>("KeyVault:DisableChallengeResourceVerification")
    };
    var reloadInterval = builder.Configuration.GetValue("KeyVault:ReloadInterval", TimeSpan.FromMinutes(30));
    builder.Configuration.AddBookstoreKeyVault(new Uri(vaultUri), credential, reloadInterval, clientOptions);
}

UnresolvedReferenceCheck.ThrowIfAny(builder.Configuration);

builder.Services.AddBookstoreOptions();

var app = builder.Build();

if (useAppConfiguration)
{
    // Refresh'i request'ler tetikler; middleware pipeline'ın başında olmalı
    app.UseAzureAppConfiguration();
}

if (app.Environment.IsDevelopment())
{
    // Hangi değer hangi provider'dan geldi; secret'lar maskeli
    app.MapGet("/debug/config", (IConfiguration config) =>
        ConfigDebug.SafeDebugView((IConfigurationRoot)config));
}

// Reports which settings are present, never their values.
app.MapGet("/api/info", (
    IHostEnvironment env,
    IConfiguration config,
    IOptionsMonitor<ShippingOptions> shipping,
    IOptionsMonitor<JwtOptions> jwt) => new
{
    Environment = env.EnvironmentName,
    shipping.CurrentValue.FreeShippingThreshold,
    // Key'in kendisi değil, SHA-256 özetinin ilk 8 karakteri
    JwtKeyFingerprint = KeyFingerprint.Of(jwt.CurrentValue.SigningKey),
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
