using System.Buffers;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Threading.Channels;
using Microsoft.Win32.SafeHandles;

namespace Bookstore.IO.LargeFiles;

public static class RevenueReaders
{
    // 1) Herkesin ilk yazdığı: her satır bir string, her alan bir string.
    public static long[] WithStreamReader(string path)
    {
        var totals = new Dictionary<DateOnly, decimal>();
        foreach (string line in File.ReadLines(path))
        {
            string[] parts = line.Split(',');
            if (parts[2] is "Pending" or "Cancelled") continue;
            var day = DateOnly.ParseExact(parts[0].AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            totals[day] = totals.GetValueOrDefault(day) + decimal.Parse(parts[3], CultureInfo.InvariantCulture);
        }

        var cents = new long[LineParser.Days];
        foreach (var (day, total) in totals) cents[day.DayNumber - OrderLog.FirstDay.DayNumber] = (long)(total * 100);
        return cents;
    }

    // 2) Tek thread, sabit boyutlu kiralık buffer, string yok.
    public static long[] Chunked(string path, int bufferSize = 1 << 20)
    {
        var cents = new long[LineParser.Days];
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0, FileOptions.SequentialScan);
            int carry = 0, read;
            while ((read = file.Read(buffer, carry, buffer.Length - carry)) > 0)
            {
                int length = carry + read;
                int consumed = LineParser.AccumulateLines(buffer.AsSpan(0, length), cents);
                // Yarım kalan satırı buffer'ın başına taşı.
                carry = length - consumed;
                buffer.AsSpan(consumed, carry).CopyTo(buffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        return cents;
    }

    // 3) Dosya offset aralıklarına bölünür; işçiler aralıkları bir Channel'dan alır ve RandomAccess ile okur.
    public static async Task<long[]> ParallelRangesAsync(string path, int workers, int rangeCount = 64)
    {
        using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        IReadOnlyList<FileRange> ranges = FileRange.Split(handle, rangeCount);
        Channel<FileRange> queue = Channel.CreateBounded<FileRange>(rangeCount);
        foreach (FileRange range in ranges) queue.Writer.TryWrite(range);
        queue.Writer.Complete();

        long[][] partials = await Task.WhenAll(Enumerable.Range(0, workers).Select(_ => Task.Run(async () =>
        {
            var cents = new long[LineParser.Days];
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1 << 20);
            try
            {
                await foreach (FileRange range in queue.Reader.ReadAllAsync())
                {
                    long offset = range.Start;
                    int carry = 0;
                    while (offset < range.End)
                    {
                        int toRead = (int)Math.Min(buffer.Length - carry, range.End - offset);
                        int read = RandomAccess.Read(handle, buffer.AsSpan(carry, toRead), offset);
                        offset += read;
                        int length = carry + read;
                        int consumed = LineParser.AccumulateLines(buffer.AsSpan(0, length), cents);
                        carry = length - consumed;
                        buffer.AsSpan(consumed, carry).CopyTo(buffer);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return cents;
        })));

        return Merge(partials);
    }

    // 4) Aynı aralıklar, ama veri kopyalanmadan doğrudan map edilmiş bellekten okunur.
    public static unsafe long[] MemoryMapped(string path, int workers, int rangeCount = 64)
    {
        IReadOnlyList<FileRange> ranges;
        using (SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            ranges = FileRange.Split(handle, rangeCount);
        }

        using var map = MemoryMappedFile.CreateFromFile(path, FileMode.Open, mapName: null, capacity: 0, MemoryMappedFileAccess.Read);
        using MemoryMappedViewAccessor view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        byte* basePointer = null;
        view.SafeMemoryMappedViewHandle.AcquirePointer(ref basePointer);
        try
        {
            byte* start = basePointer + view.PointerOffset;
            var partials = new long[ranges.Count][];
            Parallel.For(0, ranges.Count, new ParallelOptions { MaxDegreeOfParallelism = workers }, i =>
            {
                FileRange range = ranges[i];
                var cents = new long[LineParser.Days];
                var span = new ReadOnlySpan<byte>(start + range.Start, checked((int)(range.End - range.Start)));
                LineParser.AccumulateLines(span, cents);
                partials[i] = cents;
            });
            return Merge(partials);
        }
        finally
        {
            view.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    private static long[] Merge(IEnumerable<long[]> partials)
    {
        var total = new long[LineParser.Days];
        foreach (long[] part in partials)
        {
            for (int i = 0; i < total.Length; i++) total[i] += part[i];
        }
        return total;
    }
}

public readonly record struct FileRange(long Start, long End)
{
    // Splits the file into ranges whose boundaries fall right after a '\n'.
    public static IReadOnlyList<FileRange> Split(SafeFileHandle handle, int count)
    {
        long length = RandomAccess.GetLength(handle);
        var boundaries = new List<long> { 0 };
        Span<byte> probe = stackalloc byte[256];
        for (int i = 1; i < count; i++)
        {
            long guess = length * i / count;
            int read = RandomAccess.Read(handle, probe, guess);
            int newline = probe[..read].IndexOf((byte)'\n');
            long boundary = guess + newline + 1;
            if (boundary > boundaries[^1]) boundaries.Add(boundary);
        }
        boundaries.Add(length);
        return [.. boundaries.Zip(boundaries.Skip(1), (start, end) => new FileRange(start, end))];
    }
}
