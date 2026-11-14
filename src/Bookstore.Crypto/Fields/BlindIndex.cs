using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Fields;

// Keyed, deterministic lookup value for an encrypted field (equality search only)
public sealed class BlindIndex(byte[] key, int lengthInBytes = 16)
{
    public string Compute(string normalizedValue)
    {
        byte[] mac = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(normalizedValue));
        return Convert.ToBase64String(mac, 0, lengthInBytes);
    }
}
