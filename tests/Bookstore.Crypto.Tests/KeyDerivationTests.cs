using System.Text;
using Bookstore.Crypto.Keys;

namespace Bookstore.Crypto.Tests;

public class KeyDerivationTests
{
    private static readonly KeyDerivation Kdf = new(Encoding.ASCII.GetBytes("test-only-master-key-of-32-bytes"));

    [Fact]
    public void Same_purpose_gives_the_same_key() =>
        Assert.Equal(Kdf.DeriveKey(KeyPurposes.CustomerPhoneEncryption), Kdf.DeriveKey(KeyPurposes.CustomerPhoneEncryption));

    [Fact]
    public void Different_purposes_and_institutions_give_unrelated_keys()
    {
        byte[][] keys =
        [
            Kdf.DeriveKey(KeyPurposes.CustomerPhoneEncryption),
            Kdf.DeriveKey(KeyPurposes.CustomerPhoneBlindIndex),
            Kdf.DeriveKey(KeyPurposes.InstitutionWebhook(Guid.CreateVersion7())),
            Kdf.DeriveKey(KeyPurposes.InstitutionWebhook(Guid.CreateVersion7())),
        ];

        Assert.Equal(keys.Length, keys.Select(Convert.ToHexString).Distinct().Count());
    }

    [Fact]
    public void A_short_master_key_is_refused() =>
        Assert.Throws<ArgumentException>(() => new KeyDerivation(new byte[16]));
}
