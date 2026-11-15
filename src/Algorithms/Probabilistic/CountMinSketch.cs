namespace Algorithms.Probabilistic;

// d rows of w counters. An estimate is never below the true count; it is above it by at most epsilon * N with probability 1 - delta
public sealed class CountMinSketch
{
    private readonly long[,] _counts;

    public CountMinSketch(int width, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);
        _counts = new long[depth, width];
        Width = width;
        Depth = depth;
    }

    public int Width { get; }

    public int Depth { get; }

    public long TotalCount { get; private set; }

    public long SizeInBytes => (long)Width * Depth * sizeof(long);

    // w = ceil(e / epsilon), d = ceil(ln(1 / delta))
    public static CountMinSketch ForError(double epsilon, double delta) =>
        new((int)Math.Ceiling(Math.E / epsilon), (int)Math.Ceiling(Math.Log(1 / delta)));

    public void Add(string item, long count = 1)
    {
        (ulong h1, ulong h2) = ItemHash.Hash128(item);
        for (int row = 0; row < Depth; row++) _counts[row, Column(h1, h2, row)] += count;
        TotalCount += count;
    }

    public long Estimate(string item)
    {
        (ulong h1, ulong h2) = ItemHash.Hash128(item);
        long min = long.MaxValue;
        for (int row = 0; row < Depth; row++) min = Math.Min(min, _counts[row, Column(h1, h2, row)]);
        return min; // every row over-counts because of collisions; the smallest is the closest
    }

    private int Column(ulong h1, ulong h2, int row) => (int)((h1 + (ulong)row * h2) % (ulong)Width);
}
