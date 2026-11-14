using System.Buffers.Text;
using System.Security.Cryptography;
using Bookstore.Crypto.Hybrid;

namespace Bookstore.Crypto.Tests;

public sealed class PqHybridTests : IDisposable
{
    private static readonly byte[] Order = """{"purchaseOrder":"PO-TEST-1","lines":[{"isbn":"9780441172719","quantity":3}]}"""u8.ToArray();
    private readonly ECDiffieHellman _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

    public void Dispose() => _ecdh.Dispose();

    [Fact]
    public void Classic_envelope_still_opens_through_the_reader()
    {
        HybridEnvelope envelope = EcdhHybrid.Encrypt(_ecdh, "test/2026", Order);
        Assert.Equal(Order, EnvelopeReader.Open(new RecipientKeys(_ecdh), envelope));
    }

    [Fact]
    public void Reader_rejects_an_algorithm_that_is_not_on_the_allow_list()
    {
        HybridEnvelope envelope = EcdhHybrid.Encrypt(_ecdh, "test/2026", Order) with { Algorithm = "RSA-PKCS1.A128CBC" };
        CryptographicException ex = Assert.Throws<CryptographicException>(() => EnvelopeReader.Open(new RecipientKeys(_ecdh), envelope));
        Assert.Contains("not accepted", ex.Message);
    }

    [Fact]
    public void Hybrid_envelope_round_trips()
    {
        Assert.SkipUnless(MLKem.IsSupported, "ML-KEM needs OpenSSL 3.5+ or Windows CNG with PQC");
        using MLKem kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);

        HybridEnvelope envelope = PqHybrid.Encrypt(kem, _ecdh, "test/2027-pq", Order);

        Assert.Equal(PqHybrid.Algorithm, envelope.Algorithm);
        Assert.Equal(Order, EnvelopeReader.Open(new RecipientKeys(_ecdh, kem), envelope));
    }

    [Fact]
    public void Both_halves_of_the_hybrid_key_are_needed()
    {
        Assert.SkipUnless(MLKem.IsSupported, "ML-KEM needs OpenSSL 3.5+ or Windows CNG with PQC");
        using MLKem kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
        using MLKem otherKem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
        using ECDiffieHellman otherEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        HybridEnvelope envelope = PqHybrid.Encrypt(kem, _ecdh, "test/2027-pq", Order);

        Assert.ThrowsAny<CryptographicException>(() => PqHybrid.Decrypt(otherKem, _ecdh, envelope));
        Assert.ThrowsAny<CryptographicException>(() => PqHybrid.Decrypt(kem, otherEcdh, envelope));
    }

    [Fact]
    public void Changed_ml_kem_ciphertext_is_detected()
    {
        Assert.SkipUnless(MLKem.IsSupported, "ML-KEM needs OpenSSL 3.5+ or Windows CNG with PQC");
        using MLKem kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
        HybridEnvelope envelope = PqHybrid.Encrypt(kem, _ecdh, "test/2027-pq", Order);
        byte[] encapsulated = Base64Url.DecodeFromChars(envelope.EncapsulatedKey);
        encapsulated[10] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() =>
            PqHybrid.Decrypt(kem, _ecdh, envelope with { EncapsulatedKey = Base64Url.EncodeToString(encapsulated) }));
    }
}
