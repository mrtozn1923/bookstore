using System.IO.MemoryMappedFiles;
using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class MapLabTests
{
    [Fact]
    public void Copy_on_write_view_does_not_change_the_file()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");
        string path = Path.Combine(Path.GetTempPath(), $"cow-{Guid.NewGuid():N}.csv");
        try
        {
            string[] lines = MapLab.CopyOnWrite(path, 4).ToArray();

            Assert.Equal("in view: '#'  on disk: '9'", lines[^1]);
            Assert.Equal((byte)'9', File.ReadAllBytes(path)[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Reading_a_mapped_file_adds_resident_file_pages()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");
        string path = Path.Combine(Path.GetTempPath(), $"map-{Guid.NewGuid():N}.csv");
        try
        {
            MapLab.CreateCatalogExport(path, 16);
            long before = MapLab.Rollup()["Rss"];
            using MemoryMappedFile file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using MemoryMappedViewAccessor view = file.CreateViewAccessor(0, 16 << 20, MemoryMappedFileAccess.Read);
            MapLab.ReadEveryPage(view, 16 << 20);
            long after = MapLab.Rollup()["Rss"];

            // 16 MB'ın tamamı okundu: en az 14 MB RSS'e eklenmeli
            Assert.True(after - before >= 14 * 1024, $"before={before} kB, after={after} kB");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Gc_reserve_shows_up_as_a_large_inaccessible_mapping()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");

        string[] large = MapLab.LargeMappings(256).ToArray();

        Assert.Contains(large, line => line.Contains("---p"));
    }

    [Fact]
    public void Write_xor_execute_keeps_mappings_from_being_both_writable_and_executable()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");
        Assert.SkipWhen(Environment.GetEnvironmentVariable("DOTNET_EnableWriteXorExecute") == "0", "W^X disabled");

        Assert.Equal(0, MapLab.Permissions().Both);
    }

    [Fact]
    public void Container_report_reads_cgroup_files()
    {
        Assert.SkipUnless(File.Exists("/sys/fs/cgroup/memory.max"), "needs cgroup v2");

        Assert.NotEqual("n/a", MemLab.CgroupValue("memory.current"));
        Assert.Contains("memory.current=", MemLab.Report());
    }
}
