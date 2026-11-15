using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Privacy.Protection;

public sealed class PiiProtector(PiiProtectionOptions options)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    // Ciphertext format: "<keyId>.<base64(nonce | ciphertext | tag)>"
    public string Encrypt(string plaintext, string purpose)
    {
        byte[] key = options.EncryptionKeys[options.ActiveKeyId];
        byte[] input = Encoding.UTF8.GetBytes(plaintext);
        byte[] output = new byte[NonceSize + input.Length + TagSize];
        Span<byte> nonce = output.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, input, output.AsSpan(NonceSize, input.Length), output.AsSpan(NonceSize + input.Length), Encoding.UTF8.GetBytes(purpose));
        return $"{options.ActiveKeyId}.{Convert.ToBase64String(output)}";
    }

    public string Decrypt(string protectedValue, string purpose)
    {
        int dot = protectedValue.IndexOf('.');
        byte[] key = options.EncryptionKeys[protectedValue[..dot]];
        byte[] data = Convert.FromBase64String(protectedValue[(dot + 1)..]);
        int length = data.Length - NonceSize - TagSize;
        byte[] plaintext = new byte[length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(data.AsSpan(0, NonceSize), data.AsSpan(NonceSize, length), data.AsSpan(NonceSize + length), plaintext, Encoding.UTF8.GetBytes(purpose));
        return Encoding.UTF8.GetString(plaintext);
    }

    // Deterministic keyed hash for equality lookups; without the key it cannot be recomputed.
    public string BlindIndex(string normalized, string purpose)
    {
        byte[] mac = HMACSHA256.HashData(options.BlindIndexKey, Encoding.UTF8.GetBytes($"{purpose}:{normalized}"));
        return Convert.ToHexStringLower(mac.AsSpan(0, 16));
    }
}
