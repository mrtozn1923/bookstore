using Bookstore.Catalog;
using Bookstore.FileDrops;
using Bookstore.Inventory.SupplierFeed;
using Bookstore.Ordering;
using Bookstore.Payments.BankStatements;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookstore.Api.Tests;

// The processors against a local folder instead of SFTP/FTPS: same port, no server needed.
public sealed class FileDropTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("bookstore-drops-").FullName;
    private readonly CatalogStore _catalog = new();
    private readonly OrderStore _orders = new();

    public FileDropTests()
    {
        foreach (var folder in new[] { "outbound/processed", "inbound", "statements/processed" })
        {
            Directory.CreateDirectory(Path.Combine(_root, "remote", folder));
        }
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private FileDropLedger Ledger() => new(Path.Combine(_root, "work", "ledger.jsonl"));

    private SupplierFeedProcessor Feed(FileDropLedger ledger) => new(new LocalFolderFileDrop(Path.Combine(_root, "remote")),
        ledger, _catalog, new FileDropFolders(Path.Combine(_root, "work")), TimeProvider.System, NullLogger<SupplierFeedProcessor>.Instance);

    private void Drop(string path, string content, bool marker = false)
    {
        File.WriteAllText(Path.Combine(_root, "remote", path), content);
        if (marker)
        {
            File.WriteAllText(Path.Combine(_root, "remote", path + ".done"), "");
        }
    }

    private const string Feed1114 = "isbn;list_price;currency;stock\n9780441013593;479,00;TRY;20\n9780141439549;245,00;EUR;5\n";

    [Fact]
    public async Task Feed_is_applied_once_and_bad_rows_are_reported()
    {
        Drop("outbound/stock-prices-20261114.csv", Feed1114, marker: true);
        Drop("outbound/stock-prices-20261115.csv", "isbn;list_price;currency;stock\n9780441013593;48");

        var results = await Feed(Ledger()).RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal([FeedFileStatus.Processed, FeedFileStatus.WaitingForMarker], results.Select(r => r.Status));
        Assert.Equal((479.00m, 20), (_catalog.Find("9780441013593")!.Price, _catalog.Find("9780441013593")!.Stock));
        Assert.Equal("line;isbn;error\n3;9780141439549;unsupported currency\n",
            File.ReadAllText(Path.Combine(_root, "remote", "inbound", "stock-prices-20261114.errors.csv")));
        Assert.True(File.Exists(Path.Combine(_root, "remote", "outbound", "processed", "stock-prices-20261114.csv")));
    }

    [Fact]
    public async Task Resent_file_with_same_content_is_not_applied_again_even_after_a_restart()
    {
        Drop("outbound/stock-prices-20261114.csv", Feed1114, marker: true);
        await Feed(Ledger()).RunAsync(TestContext.Current.CancellationToken);
        _catalog.Find("9780441013593")!.Stock = 19; // a book was sold in the meantime

        Drop("outbound/stock-prices-20261114-resend.csv", Feed1114, marker: true);
        // A new ledger instance reads the file again, like the API after a restart.
        var results = await Feed(Ledger()).RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(FeedFileStatus.AlreadyProcessed, Assert.Single(results).Status);
        Assert.Equal(19, _catalog.Find("9780441013593")!.Stock);
    }

    [Fact]
    public async Task Bank_lines_are_matched_by_reference_and_amount_and_never_twice()
    {
        var bank = new BankStatementReconciler(new LocalFolderFileDrop(Path.Combine(_root, "remote")), Ledger(), _orders,
            new FileDropFolders(Path.Combine(_root, "work")), TimeProvider.System, NullLogger<BankStatementReconciler>.Instance);
        Drop("statements/collections-20261114.csv", "transaction_id;value_date;amount;currency;description\n" +
            "TX-1;2026-11-14;4131.00;TRY;BKS-1003 School 0042 book order\n" +
            "TX-2;2026-11-14;2000.00;TRY;bks 1004 library payment\n" +
            "TX-3;2026-11-14;980.00;TRY;Payment for books\n");

        var first = await bank.RunAsync(TestContext.Current.CancellationToken);
        Drop("statements/collections-20261115.csv", "transaction_id;value_date;amount;currency;description\nTX-1;2026-11-14;4131.00;TRY;BKS-1003 School 0042 book order\n");
        var second = await bank.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal([MatchOutcome.Matched, MatchOutcome.AmountMismatch, MatchOutcome.UnknownReference], first.Select(l => l.Outcome));
        Assert.Equal(OrderStatus.Paid, _orders.Find(1003)!.Status);
        Assert.Equal(MatchOutcome.AlreadySeen, Assert.Single(second).Outcome);
    }

    [Theory]
    [InlineData("9780441013593", true)]
    [InlineData("9780000000001", false)]
    [InlineData("978044101359", false)]
    public void Isbn13_check_digit(string isbn, bool valid) => Assert.Equal(valid, SupplierFeedProcessor.IsValidIsbn13(isbn));
}
