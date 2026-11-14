using Azure.Security.KeyVault.Secrets;
using Bookstore.Api.KeyVault;

namespace Bookstore.Api.Tests;

public class PrefixKeyVaultSecretManagerTests
{
    private readonly PrefixKeyVaultSecretManager _manager = new("BookstoreApi");

    [Theory]
    [InlineData("BookstoreApi-Jwt--SigningKey", true)]
    [InlineData("bookstoreapi-Email--ApiKey", true)]
    [InlineData("NotificationsWorker-Email--ApiKey", false)]
    [InlineData("Jwt--SigningKey", false)]
    public void Load_accepts_only_this_apps_secrets(string name, bool expected)
    {
        var properties = SecretModelFactory.SecretProperties(name: name);

        Assert.Equal(expected, _manager.Load(properties));
    }

    [Fact]
    public void Load_skips_expired_secrets()
    {
        var properties = new SecretProperties("BookstoreApi-Jwt--SigningKey")
        {
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        Assert.False(_manager.Load(properties));
    }

    [Fact]
    public void GetKey_strips_prefix_and_maps_double_dash_to_colon()
    {
        var secret = SecretModelFactory.KeyVaultSecret(
            SecretModelFactory.SecretProperties(name: "BookstoreApi-ConnectionStrings--Bookstore"), "value");

        Assert.Equal("ConnectionStrings:Bookstore", _manager.GetKey(secret));
    }
}
