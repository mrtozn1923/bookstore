using System.Buffers;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;

namespace Bookstore.IO.LargeFiles;

public static class LargeFileDemos
{
    public const string LogPath = "data/order-events.log";

    public static void Generate(double gigabytes)
    {
        Directory.CreateDirectory("data");
        var sw = Stopwatch.StartNew();
        long lines = OrderLog.Generate(LogPath, (long)(gigabytes * 1_000_000_000));
        Console.WriteLine($"{lines:N0} lines, {new FileInfo(LogPath).Length:N0} bytes in {sw.Elapsed.TotalSeconds:N1} s");
        foreach (string line in File.ReadLines(LogPath).Take(3)) Console.WriteLine($"  {line}");
    }

    public static async Task RevenueAsync(string mode, int workers)
    {
        long peakWorkingSet = 0;
        using var sampler = new Timer(_ => peakWorkingSet = Math.Max(peakWorkingSet, Environment.WorkingSet), null, 0, 20);
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        int gen0 = GC.CollectionCount(0), gen2 = GC.CollectionCount(2);
        var sw = Stopwatch.StartNew();

        long[] cents = mode switch
        {
            "streamreader" => RevenueReaders.WithStreamReader(LogPath),
            "chunked" => RevenueReaders.Chunked(LogPath),
            "parallel" => await RevenueReaders.ParallelRangesAsync(LogPath, workers),
            "mmap" => RevenueReaders.MemoryMapped(LogPath, workers),
            _ => throw new ArgumentException($"Unknown mode '{mode}'.")
        };

        sw.Stop();
        peakWorkingSet = Math.Max(peakWorkingSet, Environment.WorkingSet);
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        int days = cents.Count(c => c > 0);
        string label = mode is "parallel" or "mmap" ? $"{mode} x{workers}" : mode;
        Console.WriteLine($"{label,-13} {sw.Elapsed.TotalSeconds,6:N2} s  allocated {allocated / 1_048_576.0,9:N1} MB  " +
                          $"peak working set {peakWorkingSet / 1_048_576.0,7:N0} MB  gen0 {GC.CollectionCount(0) - gen0,5}  gen2 {GC.CollectionCount(2) - gen2,3}");
        Console.WriteLine($"  {days} days, total {cents.Sum() / 100m:N2} TRY, {OrderLog.FirstDay:yyyy-MM-dd}: {cents[0] / 100m:N2} TRY");
    }

    // Rastgele erişim: dosyanın rastgele yerlerinden 64 byte okumak.
    public static unsafe void Random(string mode, int count)
    {
        long length = new FileInfo(LogPath).Length;
        var random = new Random(42);
        long[] offsets = Enumerable.Range(0, count).Select(_ => random.NextInt64(length - 64)).ToArray();
        Span<byte> window = stackalloc byte[64];
        long checksum = 0;
        var sw = Stopwatch.StartNew();
        if (mode == "randomaccess")
        {
            using SafeFileHandle handle = File.OpenHandle(LogPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.RandomAccess);
            foreach (long offset in offsets)
            {
                RandomAccess.Read(handle, window, offset);
                checksum += window[0];
            }
        }
        else
        {
            using var map = MemoryMappedFile.CreateFromFile(LogPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using MemoryMappedViewAccessor view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            byte* basePointer = null;
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref basePointer);
            try
            {
                foreach (long offset in offsets)
                {
                    new ReadOnlySpan<byte>(basePointer + view.PointerOffset + offset, 64).CopyTo(window);
                    checksum += window[0];
                }
            }
            finally
            {
                view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
        Console.WriteLine($"{mode,-12} {count:N0} random reads in {sw.ElapsedMilliseconds:N0} ms ({sw.Elapsed.TotalNanoseconds / count:N0} ns/read), checksum {checksum}");
    }

    public static void LargeObjectHeap()
    {
        const int size = 1 << 20;
        Run("new byte[1 MB]", () => { byte[] b = new byte[size]; b[0] = 1; });
        Run("ArrayPool.Rent", () => { byte[] b = ArrayPool<byte>.Shared.Rent(size); b[0] = 1; ArrayPool<byte>.Shared.Return(b); });

        static void Run(string label, Action action)
        {
            GC.Collect();
            int gen2 = GC.CollectionCount(2);
            long allocated = GC.GetTotalAllocatedBytes(true);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5_000; i++) action();
            Console.WriteLine($"{label,-15} 5,000 x: {sw.ElapsedMilliseconds,4} ms, allocated {(GC.GetTotalAllocatedBytes(true) - allocated) / 1_048_576,5:N0} MB, gen2 collections {GC.CollectionCount(2) - gen2}");
        }
    }
}
