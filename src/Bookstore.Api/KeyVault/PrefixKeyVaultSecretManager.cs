using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace Bookstore.Api.KeyVault;

// "BookstoreApi-Jwt--SigningKey" → "Jwt:SigningKey"; başka uygulamanın secret'ları yüklenmez
public sealed class PrefixKeyVaultSecretManager(string appPrefix) : KeyVaultSecretManager
{
    private readonly string _prefix = $"{appPrefix}-";

    public override bool Load(SecretProperties secret) =>
        secret.Name.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase)
        && (secret.ExpiresOn is null || secret.ExpiresOn > DateTimeOffset.UtcNow);

    public override string GetKey(KeyVaultSecret secret) =>
        secret.Name[_prefix.Length..].Replace("--", ConfigurationPath.KeyDelimiter);
}
