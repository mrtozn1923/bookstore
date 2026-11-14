using System.Security.Cryptography;
using System.Text;
using Bookstore.Crypto;

namespace Bookstore.CryptoLab;

public static partial class Commands
{
    public static void Capabilities()
    {
        Console.WriteLine(CryptoCapabilities.DescribePlatform());
        IReadOnlyList<CryptoCapability> capabilities = CryptoCapabilities.Probe();
        foreach (CryptoCapability c in capabilities)
        {
            Console.WriteLine($"  {c.Name,-18} {(c.IsSupported ? "yes" : "no")}");
        }

        // At startup: if an algorithm is missing, stop here, not on the first request
        CryptoRequirements.EnsureSupported(capabilities, CryptoRequirements.Bookstore);
        Console.WriteLine("Bookstore requirements: OK");
    }

    public static void Providers()
    {
        using RSA rsa = RSA.Create(2048);
        using Aes aes = Aes.Create();
        using SHA256 sha256 = SHA256.Create();
        Console.WriteLine(rsa.GetType().FullName);
        Console.WriteLine(aes.GetType().FullName);
        Console.WriteLine(sha256.GetType().FullName);

        // Platform-specific type: without this guard the CA1416 analyzer breaks the build
        if (OperatingSystem.IsWindows())
        {
            using RSACng cng = new(2048);
            Console.WriteLine($"RSACng, provider: {cng.Key.Provider}");
        }
        else
        {
            Console.WriteLine("RSACng: Windows only, skipped");
        }
    }

    public static void HashApi()
    {
        byte[] isbn = Encoding.UTF8.GetBytes("978-0441013593");
        const int Iterations = 100_000;

        // Old style: create an instance every time, call ComputeHash that returns a byte[]
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
        {
            using SHA256 sha = SHA256.Create();
            _ = sha.ComputeHash(isbn);
        }
        long instanceBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        // One-shot: static method, result goes into a stack buffer
        before = GC.GetAllocatedBytesForCurrentThread();
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        for (int i = 0; i < Iterations; i++)
        {
            SHA256.HashData(isbn, digest);
        }
        long oneShotBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Console.WriteLine($"instance + ComputeHash : {instanceBytes / Iterations,4} bytes/hash");
        Console.WriteLine($"SHA256.HashData (span) : {oneShotBytes / Iterations,4} bytes/hash");
        Console.WriteLine(Convert.ToHexStringLower(digest));
    }

    public static void Zeroize()
    {
        // Pinned array: the GC will not move it, so no stray copies are left behind
        byte[] key = GC.AllocateArray<byte>(32, pinned: true);
        RandomNumberGenerator.Fill(key);
        try
        {
            Console.WriteLine($"in use : {Convert.ToHexStringLower(key.AsSpan(0, 8))}...");
            // ... encrypt or sign with the key here
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        Console.WriteLine($"cleared: {Convert.ToHexStringLower(key.AsSpan(0, 8))}...");
    }
}
