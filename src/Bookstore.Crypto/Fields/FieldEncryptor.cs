using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Fields;

// Layout: [version][keyId][nonce: 12][ciphertext: n][tag: 16]
public sealed class FieldEncryptor(FieldKeyRingProvider keys)
{
    public const byte FormatVersion = 1;
    private const int HeaderSize = 2;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public FieldEncryptor(FieldKeyRing keyRing) : this(new FieldKeyRingProvider(keyRing))
    {
    }

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        FieldKeyRing keyRing = keys.Current;
        byte keyId = keyRing.ActiveKeyId;
        byte[] payload = new byte[HeaderSize + NonceSize + plaintext.Length + TagSize];
        payload[0] = FormatVersion;
        payload[1] = keyId;

        Span<byte> nonce = payload.AsSpan(HeaderSize, NonceSize);
        Span<byte> ciphertext = payload.AsSpan(HeaderSize + NonceSize, plaintext.Length);
        Span<byte> tag = payload.AsSpan(HeaderSize + NonceSize + plaintext.Length, TagSize);
        RandomNumberGenerator.Fill(nonce);

        using AesGcm aes = new(keyRing.Get(keyId), TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, BuildAad(payload.AsSpan(0, HeaderSize), associatedData));
        return payload;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> associatedData)
    {
        if (payload.Length < HeaderSize + NonceSize + TagSize || payload[0] != FormatVersion)
        {
            throw new CryptographicException("Unknown field encryption format.");
        }

        int length = payload.Length - HeaderSize - NonceSize - TagSize;
        byte[] plaintext = new byte[length];
        using AesGcm aes = new(keys.Current.Get(payload[1]), TagSize);
        aes.Decrypt(
            payload.Slice(HeaderSize, NonceSize),
            payload.Slice(HeaderSize + NonceSize, length),
            payload.Slice(HeaderSize + NonceSize + length, TagSize),
            plaintext,
            BuildAad(payload[..HeaderSize], associatedData));
        return plaintext;
    }

    public string EncryptString(string plaintext, string associatedData) =>
        Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plaintext), Encoding.UTF8.GetBytes(associatedData)));

    public string DecryptString(string payload, string associatedData) =>
        Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(payload), Encoding.UTF8.GetBytes(associatedData)));

    public static byte KeyIdOf(string payload) => Convert.FromBase64String(payload)[1];

    // The header is authenticated too: changing version or keyId breaks the tag
    private static byte[] BuildAad(ReadOnlySpan<byte> header, ReadOnlySpan<byte> associatedData) => [.. header, .. associatedData];
}
