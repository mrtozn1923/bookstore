using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Bookstore.Api.KeyVault;

public static class KeyVaultConfigurationExtensions
{
    public const string AppPrefix = "BookstoreApi";

    // Production'da yalnız managed identity; geliştirmede geliştiricinin kendi Azure oturumu
    public static TokenCredential CreateCredential(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment())
        {
            return new ChainedTokenCredential(new AzureCliCredential(), new VisualStudioCredential());
        }

        return configuration["KeyVault:ManagedIdentityClientId"] is { Length: > 0 } clientId
            ? new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId))
            : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
    }

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
