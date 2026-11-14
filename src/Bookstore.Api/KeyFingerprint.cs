using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Api;

public static class KeyFingerprint
{
    public static string Of(string value) =>
        string.IsNullOrEmpty(value)
            ? ""
            : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..8];
}
