using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Bookstore.OsLab;

public sealed record MemSnapshot(long VirtualMb, long ResidentMb, long MinorFaults, long MajorFaults)
{
    public override string ToString() =>
        $"virtual={VirtualMb,6} MB  resident={ResidentMb,5} MB  minflt={MinorFaults,7}  majflt={MajorFaults}";
}

public static class MemLab
{
    public static MemSnapshot Snapshot()
    {
        using Process self = Process.GetCurrentProcess();
        long minor = -1, major = -1;
        if (OperatingSystem.IsLinux())
        {
            // /proc/self/stat: 10. alan minflt, 12. alan majflt
            string stat = File.ReadAllText("/proc/self/stat");
            string[] rest = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');
            minor = long.Parse(rest[7]);
            major = long.Parse(rest[9]);
        }
        return new MemSnapshot(self.VirtualMemorySize64 >> 20, self.WorkingSet64 >> 20, minor, major);
    }

    // Her sayfaya bir byte yazar; ilk dokunuş page fault üretir
    public static void TouchPages(IntPtr block, long size)
    {
        int page = Environment.SystemPageSize;
        for (long offset = 0; offset < size; offset += page)
        {
            Marshal.WriteByte(block + (nint)offset, 1);
        }
    }

    public static IEnumerable<string> DemandPaging(int megabytes)
    {
        long size = (long)megabytes << 20;
        yield return $"start          {Snapshot()}";

        IntPtr block = Marshal.AllocHGlobal((nint)size);
        yield return $"allocated      {Snapshot()}";

        TouchPages(block, size);
        yield return $"touched        {Snapshot()}";

        TouchPages(block, size);
        yield return $"touched again  {Snapshot()}";

        Marshal.FreeHGlobal(block);
        yield return $"freed          {Snapshot()}";
    }
    // cgroup v2: container'ın sayacı ve limiti (limit yoksa "max")
    public static string CgroupValue(string file)
    {
        string path = Path.Combine("/sys/fs/cgroup", file);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : "n/a";
    }

    public static string Report()
    {
        using Process self = Process.GetCurrentProcess();
        GCMemoryInfo info = GC.GetGCMemoryInfo();
        string current = CgroupValue("memory.current");
        string cgroupMb = long.TryParse(current, out long bytes) ? $"{bytes >> 20} MB" : current;
        return $"GC.GetTotalMemory={GC.GetTotalMemory(false) >> 20,4} MB  " +
               $"GC committed={info.TotalCommittedBytes >> 20,4} MB  " +
               $"RSS={self.WorkingSet64 >> 20,4} MB  memory.current={cgroupMb}";
    }

    public static IEnumerable<string> FillManaged(int megabytes)
    {
        GCMemoryInfo info = GC.GetGCMemoryInfo();
        yield return $"TotalAvailableMemoryBytes = {info.TotalAvailableMemoryBytes >> 20} MB, memory.max = {CgroupValue("memory.max")}";

        List<byte[]> cart = [];
        string? failure = null;
        for (int i = 1; i <= megabytes; i++)
        {
            try
            {
                byte[] chunk = new byte[1 << 20];
                // Sayfalara gerçekten yaz ki RSS'e yansısın
                Array.Fill(chunk, (byte)1);
                cart.Add(chunk);
            }
            catch (OutOfMemoryException ex)
            {
                failure = $"OutOfMemoryException after {cart.Count} MB: {ex.Message}";
                break;
            }
            if (i % 64 == 0)
            {
                yield return $"{i,5} MB  {Report()}";
            }
        }
        yield return failure ?? $"allocated {cart.Count} MB without error";
        yield return $"at failure  {Report()}";
        cart.Clear();
        GC.Collect();
        yield return $"after GC    {Report()}";
    }
    // Kısa ömürlü allocation yükü: toplam "totalMb" kadar 64 KB'lık array, son 20 MB'ı canlı tutulur
    public static string Churn(int totalMb)
    {
        const int chunk = 64 * 1024;
        Queue<byte[]> window = new();
        long count = (long)totalMb * 1024 * 1024 / chunk;
        long peakRss = 0;
        using Process self = Process.GetCurrentProcess();
        for (long i = 0; i < count; i++)
        {
            window.Enqueue(new byte[chunk]);
            if (window.Count > 320)
            {
                window.Dequeue();
            }
            if (i % 1024 == 0)
            {
                self.Refresh();
                peakRss = Math.Max(peakRss, self.WorkingSet64);
            }
        }
        GCMemoryInfo info = GC.GetGCMemoryInfo();
        return $"gen0 GCs={GC.CollectionCount(0),5}  GC committed={info.TotalCommittedBytes >> 20,4} MB  peak RSS={peakRss >> 20,4} MB";
    }
}
