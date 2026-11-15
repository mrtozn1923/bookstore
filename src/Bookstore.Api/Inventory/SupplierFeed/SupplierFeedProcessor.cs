using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bookstore.Catalog;
using Bookstore.FileDrops;

namespace Bookstore.Inventory.SupplierFeed;

public enum FeedFileStatus
{
    Processed,
    AlreadyProcessed,
    Failed,
    WaitingForMarker
}

public sealed record FeedFileResult(string Name, FeedFileStatus Status, int Rows = 0, int Applied = 0, int Failed = 0, string? ErrorReport = null);

// The publisher drops stock-prices-YYYYMMDD.csv into outbound/ every night and writes a .done marker when it is complete.
public sealed class SupplierFeedProcessor(
    [FromKeyedServices(SupplierFeedProcessor.DropName)] IFileDrop drop,
    FileDropLedger ledger,
    CatalogStore catalog,
    FileDropFolders folders,
    TimeProvider time,
    ILogger<SupplierFeedProcessor> logger)
{
    public const string DropName = "supplier";
    private const string Scope = "supplier-feed:file";
    private const string Header = "isbn;list_price;currency;stock";
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private readonly SemaphoreSlim _oneRunAtATime = new(1, 1);

    public async Task<IReadOnlyList<FeedFileResult>> RunAsync(CancellationToken cancellationToken)
    {
        // The polling job and the admin endpoint must not process the same file at the same time.
        await _oneRunAtATime.WaitAsync(cancellationToken);
        try
        {
            await using var session = await drop.OpenAsync(cancellationToken);
            var files = await session.ListAsync("outbound", cancellationToken);
            var markers = files.Where(f => f.Name.EndsWith(".done", StringComparison.Ordinal)).Select(f => f.Name[..^5]).ToHashSet();
            var results = new List<FeedFileResult>();
            foreach (var file in files.Where(f => f.Name.EndsWith(".csv", StringComparison.Ordinal)).OrderBy(f => f.Name))
            {
                if (!markers.Contains(file.Name))
                {
                    // Still being written (or the supplier crashed): never read a file without its marker.
                    logger.LogInformation("Skipping {File}: no .done marker yet", file.Name);
                    results.Add(new FeedFileResult(file.Name, FeedFileStatus.WaitingForMarker));
                    continue;
                }

                results.Add(await ProcessAsync(session, file.Name, cancellationToken));
            }

            return results;
        }
        finally
        {
            _oneRunAtATime.Release();
        }
    }

    private async Task<FeedFileResult> ProcessAsync(IFileDropSession session, string name, CancellationToken cancellationToken)
    {
        // 1. incoming: a local copy first, so a dropped connection never leaves a half-applied file.
        var incoming = folders.Path("supplier-feed", "incoming", name);
        await using (var local = File.Create(incoming))
        {
            await session.DownloadAsync($"outbound/{name}", local, cancellationToken);
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(incoming, cancellationToken)));
        // 2. Same content seen before (a resend, or a crash after processing): do not apply it again.
        if (ledger.Find(Scope, hash) is { } earlier)
        {
            logger.LogInformation("{File} has the same content as {Earlier}, already processed at {At}", name, earlier.Detail, earlier.At);
            File.Delete(incoming);
            await ArchiveRemoteAsync(session, name, cancellationToken);
            return new FeedFileResult(name, FeedFileStatus.AlreadyProcessed);
        }

        // 3. processing: apply the rows. Absolute values (price, stock), so applying twice gives the same result.
        var processing = folders.Path("supplier-feed", "processing", name);
        File.Move(incoming, processing, overwrite: true);
        var lines = await File.ReadAllLinesAsync(processing, Encoding.UTF8, cancellationToken);
        var errors = new List<string>();
        var applied = 0;
        var wrongFormat = lines.Length == 0 || lines[0] != Header;
        if (wrongFormat)
        {
            // Not the file we agreed on: nothing is applied, the whole file goes to failed/.
            errors.Add($"1;;expected header {Header}");
        }

        for (var i = 1; i < lines.Length && !wrongFormat; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var (error, row) = ParseRow(lines[i]);
            if (error is not null)
            {
                errors.Add($"{i + 1};{lines[i].Split(';')[0]};{error}");
                continue;
            }

            var book = catalog.Find(row.Isbn)!;
            book.Price = row.Price;
            book.Stock = row.Stock;
            applied++;
        }

        // 4. Bad rows go back to the supplier as a report, uploaded under a temporary name and renamed.
        string? report = null;
        if (errors.Count > 0)
        {
            report = Path.GetFileNameWithoutExtension(name) + ".errors.csv";
            var content = "line;isbn;error\n" + string.Join('\n', errors) + "\n";
            await session.UploadAsync($"inbound/{report}", new MemoryStream(Encoding.UTF8.GetBytes(content)), cancellationToken);
        }

        // 5. Record first, then archive: if we crash in between, the next run finds the hash and only archives.
        ledger.Add(Scope, hash, name, time.GetUtcNow());
        File.Move(processing, folders.Path("supplier-feed", wrongFormat ? "failed" : "processed", name), overwrite: true);
        await ArchiveRemoteAsync(session, name, cancellationToken);
        logger.LogInformation("{File}: {Applied} rows applied, {Failed} rejected", name, applied, errors.Count);
        return new FeedFileResult(name, wrongFormat ? FeedFileStatus.Failed : FeedFileStatus.Processed,
            Math.Max(lines.Length - 1, 0), applied, errors.Count, report);
    }

    private static async Task ArchiveRemoteAsync(IFileDropSession session, string name, CancellationToken cancellationToken)
    {
        await session.MoveAsync($"outbound/{name}", $"outbound/processed/{name}", cancellationToken);
        await session.MoveAsync($"outbound/{name}.done", $"outbound/processed/{name}.done", cancellationToken);
    }

    // isbn;list_price;currency;stock — the supplier's Excel export: ';' separated, decimal comma.
    private (string? Error, (string Isbn, decimal Price, int Stock) Row) ParseRow(string line)
    {
        var fields = line.Split(';');
        if (fields.Length != 4)
        {
            return ("expected 4 fields", default);
        }

        if (!IsValidIsbn13(fields[0]))
        {
            return ("invalid ISBN-13", default);
        }

        if (catalog.Find(fields[0]) is null)
        {
            return ("unknown ISBN", default);
        }

        if (!decimal.TryParse(fields[1], NumberStyles.Number, Turkish, out var price) || price <= 0)
        {
            return ("invalid price", default);
        }

        if (fields[2] != "TRY")
        {
            return ("unsupported currency", default);
        }

        if (!int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var stock))
        {
            return ("invalid stock", default);
        }

        return (null, (fields[0], price, stock));
    }

    public static bool IsValidIsbn13(string isbn) =>
        isbn.Length == 13 && isbn.All(char.IsAsciiDigit)
        && isbn.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum() % 10 == 0;
}
