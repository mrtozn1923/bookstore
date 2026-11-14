using System.Security.Cryptography;
using System.Globalization;
using System.Text.Unicode;

namespace Bookstore.ConcurrencyLab.Reports;

public sealed record OrderLine(string Isbn, int Quantity, decimal UnitPrice);
public sealed record Order(int Id, DateOnly Day, OrderLine[] Lines);

public static class SampleOrders
{
    public static Order[] Create(int count, int linesPerOrder, int seed = 42)
    {
        Random random = new(seed);
        Order[] orders = new Order[count];
        for (int i = 0; i < count; i++)
        {
            OrderLine[] lines = new OrderLine[linesPerOrder];
            for (int j = 0; j < linesPerOrder; j++)
                lines[j] = new OrderLine($"978{random.Next(0, 1_000_000_000):D10}",
                    random.Next(1, 4), random.Next(150, 900) + 0.90m);
            orders[i] = new Order(i + 1, new DateOnly(2026, 10, 1 + (int)((long)i * 31 / count)), lines);
        }
        return orders;
    }
}

public static class MonthlyReport
{
    private const int HashSize = 32;

    // Parallel part: each order's total and fingerprint depend only on that order
    public static decimal Summarize(Order order, Span<byte> fingerprint)
    {
        decimal total = 0;
        Span<byte> text = stackalloc byte[4096];
        int length = 0;
        foreach (OrderLine line in order.Lines)
        {
            decimal net = line.Quantity * line.UnitPrice * 0.95m;
            total += Math.Round(net * 1.10m, 2);
            Utf8.TryWrite(text[length..], CultureInfo.InvariantCulture,
                $"{line.Isbn};{line.Quantity};{net}\n", out int written);
            length += written;
        }
        SHA256.HashData(text[..length], fingerprint);
        return total;
    }

    // Serial part: every audit hash includes the previous one, so it cannot be split
    public static void Chain(ReadOnlySpan<byte> fingerprints, Span<byte> chain)
    {
        Span<byte> buffer = stackalloc byte[2 * HashSize];
        chain.Clear();
        for (int offset = 0; offset < fingerprints.Length; offset += HashSize)
        {
            chain.CopyTo(buffer);
            fingerprints.Slice(offset, HashSize).CopyTo(buffer[HashSize..]);
            SHA256.HashData(buffer, chain);
        }
    }

    public static (decimal Revenue, string AuditHash) Build(Order[] orders, int workers)
    {
        decimal[] totals = new decimal[orders.Length];
        byte[] fingerprints = new byte[orders.Length * HashSize];
        Parallel.For(0, orders.Length, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
        {
            totals[i] = Summarize(orders[i], fingerprints.AsSpan(i * HashSize, HashSize));
        });
        byte[] audit = new byte[HashSize];
        Chain(fingerprints, audit);
        return (totals.Sum(), Convert.ToHexString(audit)[..16]);
    }

    // Same report, but one chain per day and a final chain over the day hashes
    // (orders are sorted by day, so each day is one contiguous slice)
    public static (decimal Revenue, string AuditHash) BuildPerDay(Order[] orders, int workers)
    {
        decimal[] totals = new decimal[orders.Length];
        byte[] fingerprints = new byte[orders.Length * HashSize];
        Parallel.For(0, orders.Length, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
        {
            totals[i] = Summarize(orders[i], fingerprints.AsSpan(i * HashSize, HashSize));
        });
        int[] starts = [.. Enumerable.Range(0, orders.Length).Where(i => i == 0 || orders[i].Day != orders[i - 1].Day), orders.Length];
        byte[] dayHashes = new byte[(starts.Length - 1) * HashSize];
        Parallel.For(0, starts.Length - 1, new ParallelOptions { MaxDegreeOfParallelism = workers }, d =>
        {
            Chain(fingerprints.AsSpan(starts[d] * HashSize, (starts[d + 1] - starts[d]) * HashSize),
                dayHashes.AsSpan(d * HashSize, HashSize));
        });
        byte[] audit = new byte[HashSize];
        Chain(dayHashes, audit);
        return (totals.Sum(), Convert.ToHexString(audit)[..16]);
    }
}
