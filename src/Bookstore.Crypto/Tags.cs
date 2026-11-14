using System.Security.Cryptography;

namespace Bookstore.Crypto;

public static class Tags
{
    // Compares the expected and received tag without stopping at the first differing byte
    public static bool AreEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual) =>
        CryptographicOperations.FixedTimeEquals(expected, actual);
}
