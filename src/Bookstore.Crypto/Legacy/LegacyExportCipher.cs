using System.Security.Cryptography;

namespace Bookstore.Crypto.Legacy;

// Reads customer export files written by the old .NET Framework export tool:
// salt(16) | iv(16) | AES-256-CBC ciphertext, key = PBKDF2-HMAC-SHA1, 1000 iterations
// (the defaults of the old Rfc2898DeriveBytes(password, salt) constructor).
// Read path only: nothing new is ever written in this format.
public static class LegacyExportCipher
{
    private const int LegacyIterations = 1000;

    public static byte[] Decrypt(string password, ReadOnlySpan<byte> file)
    {
        ReadOnlySpan<byte> salt = file[..16];
        ReadOnlySpan<byte> iv = file[16..32];
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, LegacyIterations, HashAlgorithmName.SHA1, 32);
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            return aes.DecryptCbc(file[32..], iv);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
