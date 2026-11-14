using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Hybrid;

// Ephemeral-static ECDH on P-256, HKDF-SHA256 to a 256-bit key, AES-256-GCM for the data
public static class EcdhHybrid
{
    public const string Algorithm = "ECDH-P256.HKDF-SHA256.A256GCM";
    private const int Version = 1;

    public static HybridEnvelope Encrypt(ECDiffieHellman recipientPublicKey, string keyId, ReadOnlySpan<byte> plaintext)
    {
        // A fresh key pair for every message: the sender keeps no private key
        using ECDiffieHellman ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        byte[] ephemeralPublic = ephemeral.ExportSubjectPublicKeyInfo();
        byte[] shared = ephemeral.DeriveRawSecretAgreement(recipientPublicKey.PublicKey);
        byte[] key = DeriveKey(shared, ephemeralPublic, keyId);
        try
        {
            return Aead.Seal(Version, Algorithm, keyId, ephemeralPublic, key, plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(shared);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public static byte[] Decrypt(ECDiffieHellman recipientPrivateKey, HybridEnvelope envelope)
    {
        if (envelope.Version != Version || envelope.Algorithm != Algorithm)
        {
            throw new CryptographicException($"Unsupported envelope: v{envelope.Version} {envelope.Algorithm}");
        }

        byte[] ephemeralPublic = Base64Url.DecodeFromChars(envelope.EncapsulatedKey);
        using ECDiffieHellman ephemeral = ECDiffieHellman.Create();
        ephemeral.ImportSubjectPublicKeyInfo(ephemeralPublic, out _);
        if (ephemeral.ExportParameters(false).Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
        {
            throw new CryptographicException("The ephemeral key is not on P-256.");
        }

        byte[] shared = recipientPrivateKey.DeriveRawSecretAgreement(ephemeral.PublicKey);
        byte[] key = DeriveKey(shared, ephemeralPublic, envelope.KeyId);
        try
        {
            return Aead.Open(envelope, key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(shared);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    // The raw ECDH output is not uniformly random: always run it through a KDF, with a salt
    private static byte[] DeriveKey(byte[] shared, byte[] ephemeralPublic, string keyId) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, shared, 32, salt: ephemeralPublic,
            info: Encoding.UTF8.GetBytes($"bookstore/purchase-order/v1|{keyId}"));
}
