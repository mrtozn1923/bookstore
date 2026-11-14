using Bookstore.Api.KeyVault;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Api.Tests;

public class UnresolvedReferenceCheckTests
{
    [Fact]
    public void Throws_and_lists_keys_with_unresolved_references()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "@Microsoft.KeyVault(SecretUri=https://kv.example.net/secrets/a)",
                ["Shipping:FreeShippingThreshold"] = "500"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => UnresolvedReferenceCheck.ThrowIfAny(configuration));

        Assert.Equal("Unresolved Key Vault references: Jwt:SigningKey", ex.Message);
    }

    [Fact]
    public void Passes_when_all_values_are_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "resolved-value" })
            .Build();

        UnresolvedReferenceCheck.ThrowIfAny(configuration);
    }
}
