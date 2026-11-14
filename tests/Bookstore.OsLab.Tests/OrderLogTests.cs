using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class OrderLogTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("oslab-").FullName;

    [Fact]
    public void Naive_and_buffered_writers_produce_the_same_file()
    {
        string naive = Path.Combine(_dir, "naive.log");
        string buffered = Path.Combine(_dir, "buffered.log");

        OrderLog.WriteNaive(naive, 1_000);
        OrderLog.WriteBuffered(buffered, 1_000);

        Assert.Equal(File.ReadAllBytes(naive), File.ReadAllBytes(buffered));
        Assert.Equal(1_000, File.ReadAllLines(buffered).Length);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
