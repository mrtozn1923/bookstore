using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bookstore.LegacyCrypto;

// The customer export tool as it was written for .NET Framework 4.x. It still compiles on .NET 10, with warnings.
public static class LegacyExportTool
{
    public static byte[] Export(string password, string csv)
    {
        byte[] salt = new byte[16];
        using (RNGCryptoServiceProvider rng = new())
        {
            rng.GetBytes(salt);
        }

        using Rfc2898DeriveBytes kdf = new(password, salt);
        using RijndaelManaged aes = new() { KeySize = 256 };
        aes.Key = kdf.GetBytes(32);
        aes.GenerateIV();
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] data = Encoding.UTF8.GetBytes(csv);
        byte[] ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);
        return [.. salt, .. aes.IV, .. ciphertext];
    }

    public static string Checksum(byte[] file)
    {
        using SHA256Managed sha = new();
        return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
    }

    public static X509Certificate2 LoadSigningCertificate(byte[] pfx, string password) => new(pfx, password);
}
