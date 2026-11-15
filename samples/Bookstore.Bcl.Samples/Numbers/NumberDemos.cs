using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;

namespace Bookstore.Bcl.Samples.Numbers;

public static class NumberDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("numbers-guess", Guess),
        ("numbers-rounding", Rounding),
        ("numbers-format-vs-round", FormatVersusRound),
        ("numbers-decimal", DecimalHelpers),
        ("numbers-double-money", DoubleMoney),
        ("numbers-discount", Discount),
        ("numbers-generic", GenericMath),
        ("numbers-random", RandomDemo),
        ("numbers-random-threads", RandomThreads),
        ("numbers-guid", GuidDemo),
        ("numbers-bigint", BigIntegerDemo),
    ];

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static void Guess()
    {
        Console.WriteLine(Math.Round(2.5));
        Console.WriteLine(Math.Round(3.5));
        Console.WriteLine(0.1 + 0.2 == 0.3);
    }

    private static void Rounding()
    {
        decimal[] values = [2.5m, 3.5m, -2.5m, 2.4m, 2.6m];
        MidpointRounding[] modes =
        [
            MidpointRounding.ToEven, MidpointRounding.AwayFromZero, MidpointRounding.ToZero,
            MidpointRounding.ToNegativeInfinity, MidpointRounding.ToPositiveInfinity,
        ];

        Console.WriteLine($"{"value",-8}" + string.Concat(modes.Select(m => $"{m,20}")));
        foreach (decimal value in values)
            Console.WriteLine($"{value.ToString(Inv),-8}" + string.Concat(modes.Select(m => $"{Math.Round(value, m).ToString(Inv),20}")));

        Console.WriteLine($"Math.Round(2.345m, 2)                   = {Math.Round(2.345m, 2).ToString(Inv)}");
        Console.WriteLine($"Math.Round(2.345m, 2, AwayFromZero)     = {Math.Round(2.345m, 2, MidpointRounding.AwayFromZero).ToString(Inv)}");
        Console.WriteLine($"Math.Round(2.345, 2, AwayFromZero) (double) = {Math.Round(2.345, 2, MidpointRounding.AwayFromZero).ToString(Inv)}");
        Console.WriteLine($"2.345 as G17 = {2.345.ToString("G17", Inv)}");
    }

    private static void FormatVersusRound()
    {
        Console.WriteLine($"{"",-14}{"F0",6}{"\"0\"",6}{"Math.Round",12}");
        Row("decimal 2.5", 2.5m.ToString("F0", Inv), 2.5m.ToString("0", Inv), Math.Round(2.5m).ToString(Inv));
        Row("double 2.5", 2.5.ToString("F0", Inv), 2.5.ToString("0", Inv), Math.Round(2.5).ToString(Inv));
        Row("decimal 0.125", 0.125m.ToString("F2", Inv), 0.125m.ToString("0.00", Inv), Math.Round(0.125m, 2).ToString(Inv));
        Row("double 0.125", 0.125.ToString("F2", Inv), 0.125.ToString("0.00", Inv), Math.Round(0.125, 2).ToString(Inv));

        static void Row(string label, string f, string custom, string round) =>
            Console.WriteLine($"{label,-14}{f,6}{custom,6}{round,12}");
    }

    private static void DecimalHelpers()
    {
        decimal price = -12.75m;
        Console.WriteLine($"Floor    {decimal.Floor(price).ToString(Inv),8}   Ceiling {decimal.Ceiling(price).ToString(Inv),8}");
        Console.WriteLine($"Truncate {decimal.Truncate(price).ToString(Inv),8}   Round   {decimal.Round(price, 1).ToString(Inv),8}");
        Console.WriteLine($"Clamp(150, 0, 100) = {Math.Clamp(150, 0, 100)}, Clamp(-0.05m, 0m, 0.5m) = {Math.Clamp(-0.05m, 0m, 0.5m).ToString(Inv)}");
        Console.WriteLine($"Scale of 1.50m = {(1.50m).Scale}, 1.50m == 1.5m: {1.50m == 1.5m}, ToString: {1.50m.ToString(Inv)} vs {1.5m.ToString(Inv)}");
        Console.WriteLine($"decimal.MaxValue = {decimal.MaxValue.ToString(Inv)}");
        Console.WriteLine($"Math.Abs(-3) = {Math.Abs(-3)}, Math.Sign(-12.75m) = {Math.Sign(price)}, Math.Pow(1.18, 2) = {Math.Pow(1.18, 2).ToString(Inv)}");
    }

    private static void DoubleMoney()
    {
        double total = 0;
        decimal exact = 0;
        for (int i = 0; i < 10; i++)
        {
            total += 0.1;
            exact += 0.1m;
        }
        Console.WriteLine($"double  sum of ten 0.1 = {total.ToString("R", Inv)}, == 1.0: {total == 1.0}");
        Console.WriteLine($"decimal sum of ten 0.1 = {exact.ToString(Inv)}, == 1.0m: {exact == 1.0m}");

        double price = 349.90;
        Console.WriteLine($"349.90 * 3 as double  = {(price * 3).ToString("R", Inv)}");
        Console.WriteLine($"349.90m * 3 as decimal = {(349.90m * 3).ToString(Inv)}");
        Console.WriteLine($"(decimal)0.1 (double literal) = {((decimal)0.1).ToString(Inv)}");
        Console.WriteLine($"(decimal)349.90 (double literal) = {((decimal)349.90).ToString(Inv)}");
    }

    private static void Discount()
    {
        (string Title, decimal Price)[] cart = [("Dune", 349.90m), ("Emma", 129.90m), ("Middlemarch", 249.90m)];
        const decimal rate = 0.15m;

        decimal perLine = 0;
        foreach (var (title, price) in cart)
        {
            decimal raw = price * rate;
            decimal rounded = Math.Round(raw, 2, MidpointRounding.AwayFromZero);
            perLine += rounded;
            Console.WriteLine($"{title,-12} {price.ToString("F2", Inv),8} x 15% = {raw.ToString(Inv),-10} -> {rounded.ToString("F2", Inv)}");
        }

        decimal subtotal = cart.Sum(line => line.Price);
        decimal onTotal = Math.Round(subtotal * rate, 2, MidpointRounding.AwayFromZero);
        Console.WriteLine($"Sum of rounded lines   = {perLine.ToString("F2", Inv)}");
        Console.WriteLine($"Rounded on subtotal    = {onTotal.ToString("F2", Inv)} (subtotal {subtotal.ToString("F2", Inv)})");
        Console.WriteLine($"ToEven on 52.485       = {Math.Round(349.90m * rate, 2).ToString(Inv)}");
    }

    // Works for int, long, decimal, double...: any type that implements INumber<T>.
    public static T PercentOf<T>(T value, T percent) where T : INumber<T> =>
        value * percent / T.CreateChecked(100);

    private static void GenericMath()
    {
        Console.WriteLine($"int     : {PercentOf(250, 15)}");
        Console.WriteLine($"decimal : {PercentOf(349.90m, 15m).ToString(Inv)}");
        Console.WriteLine($"double  : {PercentOf(349.90, 15.0).ToString(Inv)}");
        Console.WriteLine($"byte    : {PercentOf<byte>(200, 50)}");
    }

    private static void RandomDemo()
    {
        var seeded1 = new Random(2026);
        var seeded2 = new Random(2026);
        Console.WriteLine($"new Random(2026) twice : {seeded1.Next(1000)} {seeded1.Next(1000)} {seeded1.Next(1000)} | {seeded2.Next(1000)} {seeded2.Next(1000)} {seeded2.Next(1000)}");

        var unseeded = Enumerable.Range(0, 3).Select(_ => new Random().Next(1000)).ToArray();
        Console.WriteLine($"new Random() x3 in a loop: {string.Join(" ", unseeded)}");

        string[] featured = ["Dune", "Emma", "Middlemarch", "The Hobbit", "Ulysses", "Ivanhoe"];
        var picker = new Random(7);
        Console.WriteLine($"GetItems(3), seed 7    : {string.Join(", ", picker.GetItems(featured, 3))}");
        string[] shuffled = [.. featured];
        picker.Shuffle(shuffled);
        Console.WriteLine($"Shuffle, seed 7        : {string.Join(", ", shuffled)}");

        Console.WriteLine($"Random.Shared.Next(1, 7) is thread-safe: {Random.Shared.Next(1, 7) is >= 1 and <= 6}");

        string coupon = RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 10);
        Console.WriteLine($"Coupon code length {coupon.Length}, only allowed chars: {coupon.All(c => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(c))}");
        Console.WriteLine($"RandomNumberGenerator.GetInt32(100, 1000) in range: {RandomNumberGenerator.GetInt32(100, 1000) is >= 100 and < 1000}");
    }

    private static void RandomThreads()
    {
        Console.WriteLine($"one new Random() from many threads: {DistinctValues(new Random().Next):N0} distinct of 1,000,000");
        Console.WriteLine($"Random.Shared from many threads   : {DistinctValues(Random.Shared.Next):N0} distinct of 1,000,000");

        var seeded = new Random(42);
        int zeros = 0;
        Parallel.For(0, 2_000_000, _ =>
        {
            if (seeded.Next() == 0)
                Interlocked.Increment(ref zeros);
        });
        Console.WriteLine($"one new Random(42) from many threads: Next() returned 0 {zeros:N0} times");

        static int DistinctValues(Func<int> next)
        {
            var seen = new System.Collections.Concurrent.ConcurrentDictionary<int, byte>();
            Parallel.For(0, 1_000_000, _ => seen.TryAdd(next(), 0));
            return seen.Count;
        }
    }

    private static void GuidDemo()
    {
        Guid v4 = Guid.NewGuid();
        Guid v7 = Guid.CreateVersion7();
        Console.WriteLine($"NewGuid        {v4}  Version={v4.Version}");
        Console.WriteLine($"CreateVersion7 {v7}  Version={v7.Version}");

        DateTimeOffset placed = new(2026, 11, 14, 9, 5, 7, TimeSpan.Zero);
        Guid[] ids = [.. Enumerable.Range(0, 5).Select(i => Guid.CreateVersion7(placed.AddMilliseconds(i)))];
        foreach (Guid id in ids)
            Console.WriteLine($"{id}  -> {ExtractTimestamp(id):yyyy-MM-dd HH:mm:ss.fff}");

        bool sortedAsGuid = ids.SequenceEqual(ids.Order());
        bool sortedAsString = ids.Select(g => g.ToString()).SequenceEqual(ids.Select(g => g.ToString()).Order(StringComparer.Ordinal));
        Console.WriteLine($"Order() keeps creation order: {sortedAsGuid}, string order keeps it: {sortedAsString}");

        Guid[] sameMillisecond = [.. Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7(placed))];
        Console.WriteLine($"Five ids in the same millisecond keep creation order: {sameMillisecond.SequenceEqual(sameMillisecond.Order())}");
    }

    // The first 48 bits of a version 7 UUID are the Unix time in milliseconds (big-endian).
    public static DateTimeOffset ExtractTimestamp(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes, bigEndian: true, out _);
        long milliseconds = 0;
        for (int i = 0; i < 6; i++)
            milliseconds = (milliseconds << 8) | bytes[i];
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }

    private static void BigIntegerDemo()
    {
        int unitsSold = int.MaxValue;
        int next = unitsSold + 1;
        Console.WriteLine($"int.MaxValue + 1 (unchecked) = {next}");

        try
        {
            int boom = checked(unitsSold + 1);
            Console.WriteLine(boom);
        }
        catch (OverflowException ex)
        {
            Console.WriteLine($"checked: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"Math.BigMul(int.MaxValue, int.MaxValue) = {Math.BigMul(int.MaxValue, int.MaxValue)}");
        Console.WriteLine($"Int128.MaxValue = {Int128.MaxValue}");

        BigInteger permutations = 1;
        for (int i = 2; i <= 30; i++)
            permutations *= i;
        Console.WriteLine($"30! shelf orderings = {permutations:N0}");
        Console.WriteLine($"BigInteger.Pow(2, 100) = {BigInteger.Pow(2, 100)}");
        Console.WriteLine($"Parse 978044117271978044117271 + 1 = {BigInteger.Parse("978044117271978044117271", Inv) + 1}");
    }
}
