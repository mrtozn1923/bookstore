using System.Security.Cryptography;
using System.Text;
using Bookstore.Crypto.Legacy;

namespace Bookstore.Crypto.Tests;

public class LegacyExportCipherTests
{
    // Produces a file exactly as the old .NET Framework tool did; the obsolete APIs are allowed only here
    private static byte[] LegacyFile(string password, string text)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
#pragma warning disable SYSLIB0022, SYSLIB0060 // RijndaelManaged, Rfc2898DeriveBytes constructor
        using Rfc2898DeriveBytes kdf = new(password, salt);
        using RijndaelManaged aes = new() { KeySize = 256 };
#pragma warning restore SYSLIB0022, SYSLIB0060
        aes.Key = kdf.GetBytes(32);
        aes.GenerateIV();
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] data = Encoding.UTF8.GetBytes(text);
        return [.. salt, .. aes.IV, .. encryptor.TransformFinalBlock(data, 0, data.Length)];
    }

    [Fact]
    public void Modern_apis_read_what_the_legacy_tool_wrote()
    {
        byte[] file = LegacyFile("dev-only-test-password", "1001;Ada Lovelace");
        Assert.Equal("1001;Ada Lovelace", Encoding.UTF8.GetString(LegacyExportCipher.Decrypt("dev-only-test-password", file)));
    }

    [Fact]
    public void Wrong_password_does_not_produce_the_text()
    {
        byte[] file = LegacyFile("dev-only-test-password", "1001;Ada Lovelace");
        // CBC has no authentication: a wrong key usually fails on padding, but can also return garbage
        try
        {
            Assert.NotEqual("1001;Ada Lovelace", Encoding.UTF8.GetString(LegacyExportCipher.Decrypt("dev-only-wrong", file)));
        }
        catch (CryptographicException)
        {
        }
    }
}
