using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace Bookstore.Api.KeyVault;

public static class KeyVaultConfigurationExtensions
{
    public const string AppPrefix = "BookstoreApi";

    public static IConfigurationBuilder AddBookstoreKeyVault(
        this IConfigurationBuilder builder,
        Uri vaultUri,
        TokenCredential credential,
        TimeSpan reloadInterval,
        SecretClientOptions? clientOptions = null)
    {
        var client = new SecretClient(vaultUri, credential, clientOptions ?? new SecretClientOptions());

        return builder.AddAzureKeyVault(client, new AzureKeyVaultConfigurationOptions
        {
            Manager = new PrefixKeyVaultSecretManager(AppPrefix),
            ReloadInterval = reloadInterval
        });
    }
}
