namespace Bookstore.Planning;

public static class Stats
{
    // Nearest-rank percentile; liste sıralı olmalı
    public static T Percentile<T>(IReadOnlyList<T> sorted, double p)
    {
        int rank = (int)Math.Ceiling(p / 100.0 * sorted.Count);
        return sorted[Math.Clamp(rank, 1, sorted.Count) - 1];
    }
}
