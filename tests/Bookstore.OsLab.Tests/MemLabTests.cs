using System.Runtime.InteropServices;
using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class MemLabTests
{
    [Fact]
    public void Allocation_is_lazy_until_pages_are_touched()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");
        const long size = 64L << 20;

        IntPtr block = Marshal.AllocHGlobal((nint)size);
        try
        {
            MemSnapshot afterAlloc = MemLab.Snapshot();
            MemLab.TouchPages(block, size);
            MemSnapshot afterTouch = MemLab.Snapshot();

            long pages = size / Environment.SystemPageSize;
            // Her sayfanın ilk yazımı en az bir minor fault üretir
            Assert.True(afterTouch.MinorFaults - afterAlloc.MinorFaults >= pages * 9 / 10);
            Assert.True(afterTouch.ResidentMb - afterAlloc.ResidentMb >= 50);
        }
        finally
        {
            Marshal.FreeHGlobal(block);
        }
    }

    [Fact]
    public void Second_touch_does_not_fault_again()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");
        const long size = 32L << 20;

        IntPtr block = Marshal.AllocHGlobal((nint)size);
        try
        {
            MemLab.TouchPages(block, size);
            MemSnapshot first = MemLab.Snapshot();
            MemLab.TouchPages(block, size);
            MemSnapshot second = MemLab.Snapshot();

            Assert.True(second.MinorFaults - first.MinorFaults < 1_000);
        }
        finally
        {
            Marshal.FreeHGlobal(block);
        }
    }
}
