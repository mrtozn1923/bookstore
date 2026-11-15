using System.Security.Cryptography;
using Bogus;
using Bookstore.Ordering;
using Bookstore.Privacy.Protection;
using Bookstore.TestData;

namespace Bookstore.Privacy.Tests;

public sealed class ProtectionTests
{
    private static PiiProtectionOptions Options(string active, params string[] keyIds) => new()
    {
        ActiveKeyId = active,
        EncryptionKeys = keyIds.ToDictionary(id => id, _ => RandomNumberGenerator.GetBytes(32)),
        BlindIndexKey = RandomNumberGenerator.GetBytes(32),
    };

    [Fact]
    public void Same_value_encrypts_differently_but_indexes_the_same()
    {
        var protector = new PiiProtector(Options("k1", "k1"));

        string a = protector.Encrypt("00000000000", "Orders.BillingNationalId");
        string b = protector.Encrypt("00000000000", "Orders.BillingNationalId");

        Assert.NotEqual(a, b);
        Assert.Equal(protector.BlindIndex("00000000000", "p"), protector.BlindIndex("00000000000", "p"));
        Assert.Equal("00000000000", protector.Decrypt(a, "Orders.BillingNationalId"));
    }

    [Fact]
    public void Ciphertext_does_not_decrypt_under_another_purpose()
    {
        var protector = new PiiProtector(Options("k1", "k1"));
        string ciphertext = protector.Encrypt("00000000000", "Orders.BillingNationalId");

        Assert.ThrowsAny<CryptographicException>(() => protector.Decrypt(ciphertext, "Customers.Phone"));
    }

    [Fact]
    public void Old_key_still_decrypts_after_rotation()
    {
        PiiProtectionOptions v1 = Options("k1", "k1");
        string old = new PiiProtector(v1).Encrypt("00000000000", "p");
        var v2 = new PiiProtectionOptions
        {
            ActiveKeyId = "k2",
            EncryptionKeys = new Dictionary<string, byte[]>(v1.EncryptionKeys) { ["k2"] = RandomNumberGenerator.GetBytes(32) },
            BlindIndexKey = v1.BlindIndexKey,
        };

        var rotated = new PiiProtector(v2);

        Assert.StartsWith("k2.", rotated.Encrypt("00000000000", "p"));
        Assert.Equal("00000000000", rotated.Decrypt(old, "p"));
    }

    [Fact]
    public void Synthetic_national_ids_never_pass_the_checksum()
    {
        var faker = new Faker();
        for (int i = 0; i < 10_000; i++)
        {
            string fake = SyntheticData.FakeNationalId(faker);
            Assert.Equal(11, fake.Length);
            Assert.False(NationalIdChecksum.IsValid(fake), fake);
        }
    }

    [Fact]
    public void Synthetic_customers_use_reserved_domain_only()
    {
        List<(Customer Customer, string FakeNationalId)> people = SyntheticData.Customers(200);

        Assert.All(people, p => Assert.EndsWith("@example.com", p.Customer.Email));
    }

    [Theory]
    [InlineData("12345678901", "*********01")]
    [InlineData(null, null)]
    public void Mask_keeps_only_the_last_two_digits(string? input, string? expected) =>
        Assert.Equal(expected, NationalIdVault.Mask(input));
}
