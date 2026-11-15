using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bookstore.Data;

namespace Bookstore.PrivacyLab;

public static class HashLinkDemo
{
    public static void Run()
    {
        // An "anonymous" analytics export: SHA-256 of the e-mail address instead of the address itself.
        string[] customerEmails = [.. FakePeople.All.Select((_, i) => FakePeople.ToCustomer(i, DateTime.UtcNow).Email)];
        string[] exportedHashes = [Sha256Hex("grace.demoray@example.com"), Sha256Hex("linus.fakely@example.com")];

        // Anyone holding the customer list re-identifies the rows with one dictionary.
        Dictionary<string, string> lookup = customerEmails.ToDictionary(Sha256Hex, e => e);
        foreach (string hash in exportedHashes)
        {
            Console.WriteLine($"{hash[..16]}... -> {lookup.GetValueOrDefault(hash, "(unknown)")}");
        }

        // An 11-digit national ID has at most 900,000,000 valid values (9 free digits + 2 check digits).
        const int sample = 2_000_000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < sample; i++)
        {
            SHA256.HashData(Encoding.ASCII.GetBytes((100_000_000 + i).ToString(CultureInfo.InvariantCulture)));
        }
        sw.Stop();
        double perSecond = sample / sw.Elapsed.TotalSeconds;
        double minutes = 900_000_000 / perSecond / 60;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"single core: {perSecond:N0} SHA-256/s, whole ID space: ~{minutes:N0} min"));
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant())));
}
