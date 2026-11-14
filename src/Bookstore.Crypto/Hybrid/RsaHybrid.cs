using System.Buffers.Text;
using System.Security.Cryptography;

namespace Bookstore.Crypto.Hybrid;

// A random 256-bit DEK encrypted with RSA-OAEP-SHA256, the data with AES-256-GCM
public static class RsaHybrid
{
    public const string Algorithm = "RSA-OAEP-SHA256.A256GCM";
    private const int Version = 1;

    public static HybridEnvelope Encrypt(RSA recipientPublicKey, string keyId, ReadOnlySpan<byte> plaintext)
    {
        byte[] dek = RandomNumberGenerator.GetBytes(32);
        try
        {
            byte[] wrapped = recipientPublicKey.Encrypt(dek, RSAEncryptionPadding.OaepSHA256);
            return Aead.Seal(Version, Algorithm, keyId, wrapped, dek, plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    public static byte[] Decrypt(RSA recipientPrivateKey, HybridEnvelope envelope)
    {
        if (envelope.Version != Version || envelope.Algorithm != Algorithm)
        {
            throw new CryptographicException($"Unsupported envelope: v{envelope.Version} {envelope.Algorithm}");
        }

        byte[] dek = recipientPrivateKey.Decrypt(Base64Url.DecodeFromChars(envelope.EncapsulatedKey), RSAEncryptionPadding.OaepSHA256);
        try
        {
            return Aead.Open(envelope, dek);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }
}
