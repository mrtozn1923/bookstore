using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bookstore.Crypto;

public sealed record CryptoCapability(string Name, bool IsSupported);

public static class CryptoCapabilities
{
    public static IReadOnlyList<CryptoCapability> Probe() =>
    [
        new("AES-GCM", AesGcm.IsSupported),
        new("AES-CCM", AesCcm.IsSupported),
        new("ChaCha20-Poly1305", ChaCha20Poly1305.IsSupported),
        new("SHA3-256", SHA3_256.IsSupported),
        new("SHAKE128", Shake128.IsSupported),
        new("KMAC256", Kmac256.IsSupported),
        new("ML-KEM", MLKem.IsSupported),
#pragma warning disable SYSLIB5006 // MLDsa and SlhDsa are [Experimental] in .NET 10
        new("ML-DSA", MLDsa.IsSupported),
        new("SLH-DSA", SlhDsa.IsSupported),
#pragma warning restore SYSLIB5006
    ];

    public static string DescribePlatform()
    {
        string os = RuntimeInformation.OSDescription;
        if (!OperatingSystem.IsLinux())
        {
            return os;
        }

        // OpenSSL 3 version number layout: 0xMNN00PP0
        long v = SafeEvpPKeyHandle.OpenSslVersion;
        return $"{os}, OpenSSL {v >> 28}.{(v >> 20) & 0xFF}.{(v >> 4) & 0xFF}";
    }
}
