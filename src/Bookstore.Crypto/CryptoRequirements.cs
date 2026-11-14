namespace Bookstore.Crypto;

public static class CryptoRequirements
{
    // Algorithms Bookstore cannot run without, on any platform
    public static readonly string[] Bookstore = ["AES-GCM"];

    public static void EnsureSupported(IEnumerable<CryptoCapability> capabilities, IEnumerable<string> required)
    {
        HashSet<string> supported = capabilities.Where(c => c.IsSupported).Select(c => c.Name).ToHashSet();
        string[] missing = required.Where(name => !supported.Contains(name)).ToArray();
        if (missing.Length > 0)
        {
            throw new PlatformNotSupportedException(
                $"Missing cryptographic algorithms: {string.Join(", ", missing)}. Platform: {CryptoCapabilities.DescribePlatform()}");
        }
    }
}
