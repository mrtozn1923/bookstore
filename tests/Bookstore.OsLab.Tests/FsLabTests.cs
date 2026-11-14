using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class FsLabTests
{
    private static string TempPath(string name) => Path.Combine(Path.GetTempPath(), $"{name}-{Guid.NewGuid():N}");

    [Fact]
    public void WriteOrders_writes_one_line_per_order()
    {
        string path = TempPath("orders.log");
        try
        {
            FlushResult result = FsLab.WriteOrders(path, 25, toDisk: true);

            Assert.Equal(25, File.ReadAllLines(path).Length);
            Assert.Equal("Flush(true)", result.Mode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AtomicSave_replaces_the_file_and_leaves_no_temp_file()
    {
        string path = TempPath("price-list.json");
        try
        {
            File.WriteAllText(path, "{\"Dune\":400.00}");

            FsLab.AtomicSave(path, "{\"Dune\":420.00}");

            Assert.Equal("{\"Dune\":420.00}", File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Open_file_stays_readable_after_delete_on_linux()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux delete semantics");
        string path = TempPath("stock-export.csv");

        string[] lines = FsLab.DeleteWhileOpen(path).ToArray();

        Assert.Equal("File.Exists after delete: False", lines[0]);
        Assert.Equal("still readable: Dune;Emma;The Hobbit", lines[1]);
        Assert.EndsWith("(deleted)", lines[2]);
    }

    [Fact]
    public void FileShare_None_blocks_a_second_FileStream()
    {
        string path = TempPath("daily-report.pdf");
        try
        {
            using FileStream held = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            Assert.Throws<IOException>(() => new FileStream(path, FileMode.Open, FileAccess.ReadWrite));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Open_descriptors_are_listed_under_proc()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");

        // En az stdin, stdout ve stderr açıktır
        Assert.True(FsLab.OpenDescriptors() >= 3);
    }
}
