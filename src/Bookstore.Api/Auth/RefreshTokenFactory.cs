using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Api.Auth;

public static class RefreshTokenFactory
{
    // a cryptographically secure 64-byte random value
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    // store the token's hash in the database, not the token itself
    public static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
