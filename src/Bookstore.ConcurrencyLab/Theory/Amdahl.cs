namespace Bookstore.ConcurrencyLab.Theory;

public static class Amdahl
{
    // serialFraction: share of single-thread time that cannot run in parallel
    public static double Speedup(double serialFraction, int cores) =>
        1.0 / (serialFraction + (1.0 - serialFraction) / cores);

    public static double Limit(double serialFraction) => 1.0 / serialFraction;

    // Gustafson: serial time stays fixed, parallel work grows with cores
    public static double ScaledSpeedup(double serialFraction, int cores) =>
        serialFraction + (1.0 - serialFraction) * cores;
}
