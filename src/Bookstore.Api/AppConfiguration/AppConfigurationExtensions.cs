using Azure.Core;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Bookstore.Api.AppConfiguration;

public static class AppConfigurationExtensions
{
    public const string KeyPrefix = "Bookstore:";
    public const string SentinelKey = "Bookstore:Sentinel";

    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrEmpty(configuration["AppConfig:Endpoint"])
        || !string.IsNullOrEmpty(configuration["AppConfig:ConnectionString"]);

    public static IConfigurationBuilder AddBookstoreAppConfiguration(
        this IConfigurationBuilder builder,
        IConfiguration configuration,
        IHostEnvironment environment,
        TokenCredential credential)
    {
        var label = environment.EnvironmentName;
        var refreshInterval = configuration.GetValue("AppConfig:RefreshInterval", TimeSpan.FromSeconds(30));

        return builder.AddAzureAppConfiguration(options =>
        {
            // Azure'da endpoint + managed identity; yerel emulator'de connection string
            if (configuration["AppConfig:ConnectionString"] is { Length: > 0 } connectionString)
            {
                options.Connect(connectionString);
            }
            else
            {
                options.Connect(new Uri(configuration["AppConfig:Endpoint"]!), credential);
            }

            options
                // Önce label'sız ortak değerler, sonra environment'ın label'ı: sonraki kazanır
                .Select(KeyPrefix + "*", LabelFilter.Null)
                .Select(KeyPrefix + "*", label)
                .TrimKeyPrefix(KeyPrefix)
                // Yalnız sentinel izlenir; değişince bütün ayarlar yeniden yüklenir
                .ConfigureRefresh(refresh => refresh
                    .Register(SentinelKey, label, refreshAll: true)
                    .SetRefreshInterval(refreshInterval))
                // Key Vault reference'ları uygulamanın kendi kimliğiyle çözülür
                .ConfigureKeyVault(keyVault => keyVault.SetCredential(credential));
        });
    }
}
