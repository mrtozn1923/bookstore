using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Hybrid;

// What travels to the recipient. Binary fields are Base64Url; the whole record is serialized as JSON.
public sealed record HybridEnvelope(int Version, string Algorithm, string KeyId, string EncapsulatedKey, string Nonce, string Ciphertext, string Tag);

internal static class Aead
{
    // The header fields are not secret, but they are authenticated
    public static byte[] HeaderAad(int version, string algorithm, string keyId, string encapsulatedKey) =>
        Encoding.UTF8.GetBytes($"{version}|{algorithm}|{keyId}|{encapsulatedKey}");

    public static HybridEnvelope Seal(int version, string algorithm, string keyId, byte[] encapsulatedKey, ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        string ek = Base64Url.EncodeToString(encapsulatedKey);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using AesGcm aes = new(key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, HeaderAad(version, algorithm, keyId, ek));
        return new HybridEnvelope(version, algorithm, keyId, ek,
            Base64Url.EncodeToString(nonce), Base64Url.EncodeToString(ciphertext), Base64Url.EncodeToString(tag));
    }

    public static byte[] Open(HybridEnvelope envelope, ReadOnlySpan<byte> key)
    {
        byte[] ciphertext = Base64Url.DecodeFromChars(envelope.Ciphertext);
        byte[] plaintext = new byte[ciphertext.Length];
        using AesGcm aes = new(key, 16);
        aes.Decrypt(Base64Url.DecodeFromChars(envelope.Nonce), ciphertext, Base64Url.DecodeFromChars(envelope.Tag), plaintext,
            HeaderAad(envelope.Version, envelope.Algorithm, envelope.KeyId, envelope.EncapsulatedKey));
        return plaintext;
    }
}
