using System.Text;
using Bookstore.Notifications.Storage;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Tests;

public sealed class LocalFileStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bookstore-files-" + Guid.NewGuid().ToString("N"));
    private readonly LocalFileStore _store;

    public LocalFileStoreTests() => _store = new LocalFileStore(Options.Create(new LocalFileStoreOptions { RootPath = _root }));

    [Fact]
    public async Task Saved_file_can_be_read_back()
    {
        using MemoryStream content = new(Encoding.UTF8.GetBytes("Dune x2"));
        StoredFile stored = await _store.SaveAsync("invoices/2026/BKS-2026-001042.txt", content, "text/plain", TestContext.Current.CancellationToken);

        await using Stream read = await _store.OpenReadAsync(stored.Path, TestContext.Current.CancellationToken);
        using StreamReader reader = new(read);
        Assert.Equal("Dune x2", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
        Assert.Equal(7, stored.Length);
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("reports/../../outside.txt")]
    public async Task Paths_outside_the_root_are_rejected(string path)
    {
        using MemoryStream content = new([1, 2, 3]);
        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(path, content, "application/octet-stream", TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
