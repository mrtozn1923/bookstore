using System.Security.Cryptography;

namespace Bookstore.Crypto.Fields;

// A data key (DEK) as stored: wrapped with a key encryption key (KEK)
public sealed record WrappedDataKey(byte KeyId, string KekId, byte[] WrappedKey);

public static class EnvelopeKeys
{
    public static WrappedDataKey CreateDataKey(byte keyId, string kekId, ReadOnlySpan<byte> kek)
    {
        byte[] dek = RandomNumberGenerator.GetBytes(32);
        try
        {
            return new WrappedDataKey(keyId, kekId, Wrap(dek, kek));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    public static byte[] Unwrap(WrappedDataKey dataKey, ReadOnlySpan<byte> kek)
    {
        using Aes aes = Aes.Create();
        aes.SetKey(kek);
        return aes.DecryptKeyWrapPadded(dataKey.WrappedKey);
    }

    // KEK rotation: the data stays as it is, only the 40-byte wrapped keys change
    public static WrappedDataKey Rewrap(WrappedDataKey dataKey, ReadOnlySpan<byte> oldKek, string newKekId, ReadOnlySpan<byte> newKek)
    {
        byte[] dek = Unwrap(dataKey, oldKek);
        try
        {
            return dataKey with { KekId = newKekId, WrappedKey = Wrap(dek, newKek) };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    public static FieldKeyRing ToKeyRing(byte activeKeyId, IEnumerable<WrappedDataKey> dataKeys, Func<string, byte[]> kekById) =>
        new(activeKeyId, dataKeys.ToDictionary(k => k.KeyId, k => Unwrap(k, kekById(k.KekId))));

    private static byte[] Wrap(ReadOnlySpan<byte> dek, ReadOnlySpan<byte> kek)
    {
        using Aes aes = Aes.Create();
        aes.SetKey(kek);
        return aes.EncryptKeyWrapPadded(dek);
    }
}
