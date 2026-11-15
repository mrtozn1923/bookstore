namespace Algorithms.Probabilistic;

// m bits, k hash positions per item. "false" = definitely never added; "true" = probably added
public sealed class BloomFilter
{
    private readonly ulong[] _bits;

    public BloomFilter(long bitCount, int hashCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bitCount, 64);
        ArgumentOutOfRangeException.ThrowIfLessThan(hashCount, 1);
        _bits = new ulong[(bitCount + 63) / 64];
        BitCount = _bits.LongLength * 64;
        HashCount = hashCount;
    }

    public long BitCount { get; }

    public int HashCount { get; }

    public long SizeInBytes => _bits.LongLength * sizeof(ulong);

    // n expected items, p target false positive rate: m = -n ln p / (ln 2)^2, k = (m / n) ln 2
    public static BloomFilter ForCapacity(long expectedItems, double falsePositiveRate)
    {
        double m = -expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2));
        int k = Math.Max(1, (int)Math.Round(m / expectedItems * Math.Log(2)));
        return new BloomFilter((long)Math.Ceiling(m), k);
    }

    // (1 - e^(-k n / m))^k
    public double ExpectedFalsePositiveRate(long itemsAdded) =>
        Math.Pow(1 - Math.Exp(-(double)HashCount * itemsAdded / BitCount), HashCount);

    public void Add(string item)
    {
        (ulong h1, ulong h2) = ItemHash.Hash128(item);
        for (int i = 0; i < HashCount; i++)
        {
            long bit = Position(h1, h2, i);
            _bits[bit >> 6] |= 1UL << (int)(bit & 63);
        }
    }

    public bool MightContain(string item)
    {
        (ulong h1, ulong h2) = ItemHash.Hash128(item);
        for (int i = 0; i < HashCount; i++)
        {
            long bit = Position(h1, h2, i);
            if ((_bits[bit >> 6] & (1UL << (int)(bit & 63))) == 0) return false; // one zero bit is enough
        }

        return true;
    }

    // Double hashing (Kirsch and Mitzenmacher): k positions from two hashes
    private long Position(ulong h1, ulong h2, int i) => (long)((h1 + (ulong)i * h2) % (ulong)BitCount);
}
