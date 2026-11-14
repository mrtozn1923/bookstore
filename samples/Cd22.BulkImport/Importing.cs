using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace Bookstore.Institutional.Import;

public sealed class ImportQueue
{
    private readonly Channel<Guid> channel = Channel.CreateBounded<Guid>(100);
    public ValueTask EnqueueAsync(Guid importId, CancellationToken ct) => channel.Writer.WriteAsync(importId, ct);
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}

public static class ImportStorage
{
    // demo: blob storage in a real application
    public static string PathFor(Guid importId)
    {
        string dir = Path.Combine(Path.GetTempPath(), "bookstore-imports");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{importId}.csv");
    }
}

public sealed class ImportWorker(ImportQueue queue, IServiceScopeFactory scopes, ILogger<ImportWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid importId in queue.ReadAllAsync(stoppingToken))
        {
            using IServiceScope scope = scopes.CreateScope();
            ImportProcessor processor = scope.ServiceProvider.GetRequiredService<ImportProcessor>();
            try
            {
                await processor.RunAsync(importId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Import {ImportId} failed", importId);
                await processor.MarkFailedAsync(importId, "Unexpected error while processing the file.");
            }
        }
    }
}

public sealed class ImportProcessor(BookstoreContext db, TimeProvider time)
{
    private const int BatchSize = 5000;
    private const int MaxRows = 50_000;
    private static readonly string[] ExpectedHeader = ["StaffNumber", "FullName", "Email", "UnitCode", "JoinedOn", "SpendingLimit"];

    public async Task RunAsync(Guid importId, CancellationToken ct)
    {
        InstitutionUserImport import = await db.InstitutionUserImports.SingleAsync(i => i.Id == importId, ct);
        import.Status = ImportStatus.Loading;
        await db.SaveChangesAsync(ct);

        Stopwatch sw = Stopwatch.StartNew();
        string? failure = await LoadAsync(import, ct);
        if (failure is not null)
        {
            import.Status = ImportStatus.Failed;
            import.FailureReason = failure;
            await db.SaveChangesAsync(ct);
            return;
        }
        long loadMs = sw.ElapsedMilliseconds;

        import.Status = ImportStatus.Validating;
        await db.SaveChangesAsync(ct);
        sw.Restart();
        await ValidateAsync(importId, import.InstitutionId, ct);
        long validateMs = sw.ElapsedMilliseconds;

        import.InvalidRows = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.InstitutionUserImportRows WHERE ImportId = {importId} AND Errors IS NOT NULL")
            .SingleAsync(ct);
        import.Status = ImportStatus.Validated;
        await db.SaveChangesAsync(ct);
        Console.WriteLine($"[worker] {import.TotalRows} rows loaded in {loadMs} ms, validated in {validateMs} ms");
    }

    public async Task MarkFailedAsync(Guid importId, string reason)
    {
        db.ChangeTracker.Clear();
        await db.InstitutionUserImports.Where(i => i.Id == importId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, ImportStatus.Failed).SetProperty(i => i.FailureReason, reason));
    }

    private async Task<string?> LoadAsync(InstitutionUserImport import, CancellationToken ct)
    {
        using TextFieldParser parser = new(ImportStorage.PathFor(import.Id), Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        string[]? header = parser.ReadFields();
        if (header is null || !header.SequenceEqual(ExpectedHeader, StringComparer.OrdinalIgnoreCase))
        {
            return $"Header must be: {string.Join(",", ExpectedHeader)}";
        }

        await using SqlConnection connection = new(db.Database.GetConnectionString());
        await connection.OpenAsync(ct);
        using SqlBulkCopy bulk = new(connection) { DestinationTableName = "dbo.InstitutionUserImportRows", BatchSize = BatchSize };
        DataTable batch = CreateBatchTable();
        foreach (DataColumn column in batch.Columns)
        {
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        int rowNumber = 1; // row 1 is the header
        while (!parser.EndOfData)
        {
            string[] fields;
            try
            {
                fields = parser.ReadFields() ?? [];
            }
            catch (MalformedLineException ex)
            {
                return $"Line {ex.LineNumber} is not valid CSV.";
            }

            rowNumber++;
            if (rowNumber - 1 > MaxRows)
            {
                return $"The file has more than {MaxRows} rows.";
            }

            // a row with missing columns still goes to staging; validation reports it
            batch.Rows.Add(import.Id, rowNumber,
                Field(fields, 0), Field(fields, 1), Field(fields, 2), Field(fields, 3), Field(fields, 4), Field(fields, 5));

            if (batch.Rows.Count == BatchSize)
            {
                await bulk.WriteToServerAsync(batch, ct);
                batch.Clear();
            }
        }

        if (batch.Rows.Count > 0)
        {
            await bulk.WriteToServerAsync(batch, ct);
        }

        import.TotalRows = rowNumber - 1;
        return null;
    }

    private static object Field(string[] fields, int index) =>
        index < fields.Length && fields[index].Length > 0 ? fields[index] : DBNull.Value;

    private static DataTable CreateBatchTable()
    {
        DataTable table = new();
        table.Columns.Add("ImportId", typeof(Guid));
        table.Columns.Add("RowNumber", typeof(int));
        foreach (string name in ExpectedHeader)
        {
            table.Columns.Add(name, typeof(string));
        }
        return table;
    }

    public Task ValidateAsync(Guid importId, int institutionId, CancellationToken ct)
    {
        DateOnly today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return db.Database.ExecuteSqlAsync($"""
            WITH checked AS
            (
                SELECT r.Errors, r.StaffNumber, r.FullName, r.Email, r.UnitCode,
                       u.Id AS UnitId,
                       TRY_CONVERT(date, r.JoinedOn, 23) AS JoinedOnValue,
                       TRY_CONVERT(decimal(19, 4), r.SpendingLimit) AS LimitValue,
                       COUNT(*) OVER (PARTITION BY r.Email) AS EmailCount,
                       CASE WHEN EXISTS (SELECT 1 FROM dbo.InstitutionUsers e
                                         WHERE e.InstitutionId = {institutionId} AND e.Email = r.Email)
                            THEN 1 ELSE 0 END AS EmailTaken
                FROM dbo.InstitutionUserImportRows r
                LEFT JOIN dbo.InstitutionUnits u ON u.InstitutionId = {institutionId} AND u.Code = r.UnitCode
                WHERE r.ImportId = {importId}
            )
            UPDATE checked SET Errors = NULLIF(CONCAT_WS(N'; ',
                CASE WHEN StaffNumber IS NULL OR StaffNumber NOT LIKE N'[0-9][0-9][0-9][0-9][0-9]'
                     THEN N'StaffNumber must be 5 digits' END,
                CASE WHEN NULLIF(TRIM(FullName), N'') IS NULL THEN N'FullName is required' END,
                CASE WHEN Email IS NULL OR Email NOT LIKE N'_%@_%._%' THEN N'Email is not valid' END,
                CASE WHEN JoinedOnValue IS NULL THEN N'JoinedOn must be yyyy-MM-dd'
                     WHEN JoinedOnValue > {today} THEN N'JoinedOn is in the future' END,
                CASE WHEN LimitValue IS NULL OR LimitValue <= 0 THEN N'SpendingLimit must be a positive number' END,
                CASE WHEN UnitId IS NULL THEN CONCAT(N'Unknown unit ', UnitCode) END,
                CASE WHEN Email IS NOT NULL AND EmailCount > 1
                     THEN CONCAT(N'Email appears ', EmailCount, N' times in the file') END,
                CASE WHEN EmailTaken = 1 THEN N'Institution user with this email already exists' END), N'');
            """, ct);
    }
}

public static class CsvReport
{
    // OWASP CSV Injection: turn a cell that starts like a formula into text, escape quotes
    public static string Cell(string? value)
    {
        string text = value ?? "";
        if (text.Length > 0 && "=+-@\t\r".Contains(text[0]))
        {
            text = "'" + text;
        }
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
