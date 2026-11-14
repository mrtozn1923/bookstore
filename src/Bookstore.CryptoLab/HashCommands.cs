using System.Security.Cryptography;
using System.Text;
using Bookstore.Crypto.Keys;
using Bookstore.Crypto.Webhooks;

namespace Bookstore.CryptoLab;

public static class HashCommands
{
    public static async Task CatalogChecksumAsync()
    {
        string path = Path.Combine(Path.GetTempPath(), "catalog-export.csv");
        string[] titles = ["Dune", "Emma", "The Hobbit", "Middlemarch"];
        await File.WriteAllLinesAsync(path, Enumerable.Range(1, 50_000).Select(i => $"{i},{titles[i % 4]},{100 + i % 400}.00"));

        await using (FileStream stream = File.OpenRead(path))
        {
            byte[] digest = await SHA256.HashDataAsync(stream);
            Console.WriteLine($"{stream.Length} bytes      sha256={Convert.ToHexStringLower(digest)}");
        }

        // Change one price on the first line: 101.00 -> 111.00
        byte[] bytes = await File.ReadAllBytesAsync(path);
        bytes[bytes.AsSpan().IndexOf("101.00"u8) + 1] = (byte)'1';
        Console.WriteLine($"one byte changed  sha256={Convert.ToHexStringLower(SHA256.HashData(bytes))}");
        File.Delete(path);
    }

    public static void EncodingPitfall()
    {
        string composed = "Café";          // é as one code point (NFC)
        string decomposed = "Café";       // e + combining accent (NFD)
        Console.WriteLine($"equal as string (ordinal): {string.Equals(composed, decomposed, StringComparison.Ordinal)}");
        Console.WriteLine($"UTF-8  NFC : {Hash(Encoding.UTF8.GetBytes(composed))}");
        Console.WriteLine($"UTF-8  NFD : {Hash(Encoding.UTF8.GetBytes(decomposed))}");
        Console.WriteLine($"UTF-16 NFC : {Hash(Encoding.Unicode.GetBytes(composed))}");
        Console.WriteLine($"UTF-8  NFD normalized to NFC: {Hash(Encoding.UTF8.GetBytes(decomposed.Normalize(NormalizationForm.FormC)))}");

        static string Hash(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data))[..16];
    }

    public static void WebhookDemo()
    {
        byte[] body = """{"eventId":"evt_0042","orderId":"0197f001-0000-7000-8000-000000000042","amount":450.00,"currency":"TRY","status":"captured"}"""u8.ToArray();
        DateTimeOffset now = new(2026, 11, 14, 9, 30, 0, TimeSpan.Zero);
        long t = now.ToUnixTimeSeconds();

        PaymentWebhookVerifier verifier = new(
            [DevKeys.PaymentWebhookSecret, DevKeys.PreviousPaymentWebhookSecret],
            new FixedTimeProvider(now),
            TimeSpan.FromMinutes(5));

        string header = PaymentWebhookSignature.CreateHeader(DevKeys.PaymentWebhookSecret, t, body);
        Console.WriteLine($"Payment-Signature: {header}");
        Console.WriteLine($"as sent            : {verifier.Verify(header, body)}");

        byte[] tampered = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(body).Replace("450.00", "45.00"));
        Console.WriteLine($"amount changed     : {verifier.Verify(header, tampered)}");

        string old = PaymentWebhookSignature.CreateHeader(DevKeys.PaymentWebhookSecret, t - 600, body);
        Console.WriteLine($"10 minutes old     : {verifier.Verify(old, body)}");

        string rotated = PaymentWebhookSignature.CreateHeader(DevKeys.PreviousPaymentWebhookSecret, t, body);
        Console.WriteLine($"previous secret    : {verifier.Verify(rotated, body)}");

        string truncated = header[..^32];
        Console.WriteLine($"truncated v1       : {verifier.Verify(truncated, body)}");
    }

    public static void DeriveKeys()
    {
        KeyDerivation kdf = new(DevKeys.MasterKey);
        Guid school = Guid.Parse("0197f001-0000-7000-8000-00000000a001");
        Guid library = Guid.Parse("0197f001-0000-7000-8000-00000000a002");

        Print(KeyPurposes.CustomerPhoneEncryption);
        Print(KeyPurposes.CustomerPhoneEncryption);
        Print(KeyPurposes.CustomerPhoneBlindIndex);
        Print(KeyPurposes.InstitutionWebhook(school));
        Print(KeyPurposes.InstitutionWebhook(library));

        void Print(string purpose) =>
            Console.WriteLine($"{Convert.ToHexStringLower(kdf.DeriveKey(purpose))[..16]}...  {purpose}");
    }

    public static void Kmac()
    {
        if (!Kmac256.IsSupported)
        {
            Console.WriteLine("KMAC256 is not supported on this platform");
            return;
        }

        byte[] key = new KeyDerivation(DevKeys.MasterKey).DeriveKey("bookstore/kmac-demo/v1");
        byte[] data = "978-0441013593"u8.ToArray();
        byte[] a = Kmac256.HashData(key, data, outputLength: 32, customizationString: "Bookstore.Catalog"u8);
        byte[] b = Kmac256.HashData(key, data, outputLength: 32, customizationString: "Bookstore.Ordering"u8);
        Console.WriteLine($"Catalog  : {Convert.ToHexStringLower(a)[..32]}...");
        Console.WriteLine($"Ordering : {Convert.ToHexStringLower(b)[..32]}...");
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
