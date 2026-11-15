using System.Text;
using Bookstore.Notifications.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

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
    [InlineData("covers/../../files-backup/db.bak")]
    [InlineData("/etc/passwd")]
    [InlineData("covers\\..\\..\\web.config")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("invoices/./2026//x.pdf")]
    public async Task Paths_outside_the_root_are_rejected(string path)
    {
        using MemoryStream content = new([1, 2, 3]);
        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(path, content, "application/octet-stream", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Download_url_works_until_it_expires_and_only_for_its_path()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero));
        LocalFileStore store = new(Options.Create(new LocalFileStoreOptions { RootPath = _root, UrlSigningKey = "test-signing-key-not-real" }), time);
        Uri url = await store.GetDownloadUrlAsync("invoices/2026/BKS-2026-001042.pdf", TimeSpan.FromMinutes(5), cancellationToken: TestContext.Current.CancellationToken);
        Dictionary<string, string> query = url.Query.TrimStart('?').Split('&').Select(p => p.Split('=')).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
        long expires = long.Parse(query["expires"]);

        Assert.True(store.VerifyDownload("invoices/2026/BKS-2026-001042.pdf", expires, query["name"], query["sig"]));
        Assert.False(store.VerifyDownload("invoices/2026/BKS-2026-001043.pdf", expires, query["name"], query["sig"]));
        Assert.False(store.VerifyDownload("invoices/2026/BKS-2026-001042.pdf", expires + 3600, query["name"], query["sig"]));

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.False(store.VerifyDownload("invoices/2026/BKS-2026-001042.pdf", expires, query["name"], query["sig"]));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
