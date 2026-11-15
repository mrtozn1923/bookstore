using System.IO.Hashing;
using System.Text;

namespace Algorithms.Probabilistic;

// One 128-bit XxHash per item, split into two 64-bit hashes; stable across processes and machines
internal static class ItemHash
{
    public static (ulong H1, ulong H2) Hash128(string item)
    {
        int max = Encoding.UTF8.GetMaxByteCount(item.Length);
        byte[]? rented = null;
        Span<byte> buffer = max <= 512 ? stackalloc byte[max] : (rented = new byte[max]);
        int length = Encoding.UTF8.GetBytes(item, buffer);
        UInt128 h = XxHash128.HashToUInt128(buffer[..length]);
        return ((ulong)h, (ulong)(h >> 64));
    }
}
