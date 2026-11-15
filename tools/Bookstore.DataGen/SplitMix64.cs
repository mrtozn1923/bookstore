namespace Bookstore.DataGen;

/// <summary>
/// A tiny, fully specified pseudo-random generator (SplitMix64).
/// System.Random with a seed is not guaranteed to give the same sequence on every .NET version,
/// so the dataset uses its own generator and only +, -, * and / on doubles (no Math.Log/Exp/Sin),
/// which keeps the output byte-for-byte identical on every OS and CPU.
/// </summary>
public sealed class SplitMix64(ulong seed)
{
    private ulong _state = seed;

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform double in [0, 1).</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public double Uniform(double min, double max) => min + (max - min) * NextDouble();

    /// <summary>Integer in [min, maxExclusive).</summary>
    public int Next(int min, int maxExclusive) => min + (int)(NextUInt64() % (ulong)(maxExclusive - min));

    public bool Chance(double probability) => NextDouble() < probability;

    /// <summary>Approximately standard normal: sum of 12 uniforms minus 6 (Irwin-Hall).</summary>
    public double Normal()
    {
        double sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += NextDouble();
        }
        return sum - 6.0;
    }

    public T Pick<T>(IReadOnlyList<T> items) => items[Next(0, items.Count)];

    public T PickWeighted<T>(IReadOnlyList<(T Item, double Weight)> items)
    {
        double total = 0;
        foreach ((T _, double w) in items)
        {
            total += w;
        }
        double r = NextDouble() * total;
        foreach ((T item, double w) in items)
        {
            if (r < w)
            {
                return item;
            }
            r -= w;
        }
        return items[^1].Item;
    }
}
