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
}
