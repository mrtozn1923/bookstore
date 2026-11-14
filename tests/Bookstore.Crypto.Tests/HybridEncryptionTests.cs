using System.Security.Cryptography;
using Bookstore.Crypto.Hybrid;

namespace Bookstore.Crypto.Tests;

public class HybridEncryptionTests
{
    private static readonly byte[] Order = new byte[4096];   // larger than any RSA block

    static HybridEncryptionTests() => RandomNumberGenerator.Fill(Order);

    [Fact]
    public void Ecdh_round_trip_through_a_public_key_pem()
    {
        using ECDiffieHellman supplier = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using ECDiffieHellman publicOnly = ECDiffieHellman.Create();
        publicOnly.ImportFromPem(supplier.ExportSubjectPublicKeyInfoPem());

        HybridEnvelope envelope = EcdhHybrid.Encrypt(publicOnly, "supplier-0042/2026", Order);

        Assert.Equal(Order, EcdhHybrid.Decrypt(supplier, envelope));
    }

    [Fact]
    public void Every_message_gets_a_new_ephemeral_key()
    {
        using ECDiffieHellman supplier = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        HybridEnvelope a = EcdhHybrid.Encrypt(supplier, "k", Order);
        HybridEnvelope b = EcdhHybrid.Encrypt(supplier, "k", Order);
        Assert.NotEqual(a.EncapsulatedKey, b.EncapsulatedKey);
    }

    [Fact]
    public void Another_recipient_cannot_open_it()
    {
        using ECDiffieHellman supplier = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using ECDiffieHellman other = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        HybridEnvelope envelope = EcdhHybrid.Encrypt(supplier, "supplier-0042/2026", Order);
        Assert.Throws<AuthenticationTagMismatchException>(() => EcdhHybrid.Decrypt(other, envelope));
    }

    [Fact]
    public void Header_fields_are_authenticated()
    {
        using RSA supplier = RSA.Create(3072);
        HybridEnvelope envelope = RsaHybrid.Encrypt(supplier, "supplier-0042/2026-rsa", Order);
        Assert.Throws<AuthenticationTagMismatchException>(() => RsaHybrid.Decrypt(supplier, envelope with { KeyId = "supplier-0043/2026-rsa" }));
    }

    [Fact]
    public void Rsa_alone_cannot_encrypt_the_order()
    {
        using RSA supplier = RSA.Create(3072);
        Assert.ThrowsAny<CryptographicException>(() => supplier.Encrypt(Order, RSAEncryptionPadding.OaepSHA256));
        Assert.Equal(Order, RsaHybrid.Decrypt(supplier, RsaHybrid.Encrypt(supplier, "k", Order)));
    }

    [Fact]
    public void Encrypted_pkcs8_needs_the_passphrase()
    {
        using ECDiffieHellman key = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        string pem = key.ExportEncryptedPkcs8PrivateKeyPem("test-only-passphrase",
            new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 600_000));

        using ECDiffieHellman restored = ECDiffieHellman.Create();
        Assert.ThrowsAny<CryptographicException>(() => restored.ImportFromEncryptedPem(pem, "test-only-wrong"));
        restored.ImportFromEncryptedPem(pem, "test-only-passphrase");
        Assert.Equal(key.ExportSubjectPublicKeyInfo(), restored.ExportSubjectPublicKeyInfo());
    }
}
