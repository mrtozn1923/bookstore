using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Keys;

public static class KeyPurposes
{
    public const string CustomerPhoneEncryption = "bookstore/customer-phone/encryption/v1";
    public const string CustomerPhoneBlindIndex = "bookstore/customer-phone/blind-index/v1";

    public static string InstitutionWebhook(Guid institutionId) =>
        $"bookstore/institution-webhook/{institutionId:N}/v1";
}

// Derives purpose-bound subkeys from one master key (HKDF, RFC 5869)
public sealed class KeyDerivation
{
    private readonly byte[] _masterKey;

    public KeyDerivation(ReadOnlySpan<byte> masterKey)
    {
        if (masterKey.Length < 32)
        {
            throw new ArgumentException("The master key must be at least 256 bits.", nameof(masterKey));
        }

        _masterKey = masterKey.ToArray();
    }

    public byte[] DeriveKey(string purpose, int length = 32)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, _masterKey, length, info: Encoding.UTF8.GetBytes(purpose));
    }
}
