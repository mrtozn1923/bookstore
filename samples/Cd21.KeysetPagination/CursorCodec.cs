using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;

namespace Bookstore.Ordering.Paging;

public sealed class CursorCodec(byte[] key)
{
    public string Encode<T>(T payload)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonSerializerOptions.Web);
        byte[] mac = HMACSHA256.HashData(key, json).AsSpan(0, 16).ToArray();
        return $"{Base64Url.EncodeToString(json)}.{Base64Url.EncodeToString(mac)}";
    }

    public bool TryDecode<T>(string token, [NotNullWhen(true)] out T? payload)
    {
        payload = default;
        string[] parts = token.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }
        try
        {
            byte[] json = Base64Url.DecodeFromChars(parts[0]);
            byte[] mac = Base64Url.DecodeFromChars(parts[1]);
            byte[] expected = HMACSHA256.HashData(key, json).AsSpan(0, 16).ToArray();
            if (!CryptographicOperations.FixedTimeEquals(mac, expected))
            {
                return false;
            }
            payload = JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);
            return payload is not null;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
