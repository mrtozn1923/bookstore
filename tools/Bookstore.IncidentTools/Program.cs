using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Bookstore.Data;
using Bookstore.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

// Incident tools for the Bookstore SQLite database. Usage:
//   demo-data <db>                    synthetic audit rows for the INC-2026-11-12 exercise
//   preserve  <db> <evidence-dir>     consistent copy + audit export + SHA-256 manifest, before anything changes
//   affected  <db> <fromUtc> <toUtc>  cross-institution reads from the audit log (run it on a working copy, not on the evidence)
string command = args.ElementAtOrDefault(0) ?? "";
return command switch
{
    "demo-data" when args.Length == 2 => await DemoDataAsync(args[1]),
    "preserve" when args.Length == 3 => await PreserveAsync(args[1], args[2]),
    "affected" when args.Length == 4 => await AffectedAsync(args[1], Utc(args[2]), Utc(args[3])),
    _ => Usage()
};

static async Task<int> DemoDataAsync(string dbPath)
{
    await using BookstoreContext db = Open(dbPath, readOnly: false);
    await db.Database.EnsureCreatedAsync();
    DateTime day = new(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
    List<AccessAuditEntry> rows = [];
    // Normal traffic: each institution reads its own orders.
    for (int hour = 8; hour < 18; hour++)
    {
        rows.Add(Row(day.AddHours(hour), "grace", 1, 1, "101,102,103"));
        rows.Add(Row(day.AddHours(hour).AddMinutes(20), "alan", 2, 2, "201,202"));
    }
    // The incident: a Hillcrest user changes the institution id in the URL.
    rows.Add(Row(day.AddHours(14).AddMinutes(3), "alan", 2, 1, "101,102,103"));
    rows.Add(Row(day.AddHours(14).AddMinutes(4), "alan", 2, 1, "101,102,103,104"));
    rows.Add(Row(day.AddHours(15).AddMinutes(41), "edsger", 2, 1, "101,102,103,104"));
    db.AccessAuditEntries.AddRange(rows);
    await db.SaveChangesAsync();
    Console.WriteLine($"{rows.Count} synthetic audit rows written to {Path.GetFileName(dbPath)}");
    return 0;
}

static async Task<int> PreserveAsync(string dbPath, string evidenceDir)
{
    Directory.CreateDirectory(evidenceDir);
    string copy = Path.Combine(evidenceDir, "bookstore-evidence.db");
    string export = Path.Combine(evidenceDir, "access-audit.jsonl");

    // SQLite online backup: a consistent copy even while the API is writing.
    await using (SqliteConnection source = new($"Data Source={dbPath};Mode=ReadOnly"))
    await using (SqliteConnection target = new($"Data Source={copy};Pooling=False"))
    {
        await source.OpenAsync();
        await target.OpenAsync();
        source.BackupDatabase(target);
        // A single self-contained file: no -wal/-shm side files next to the evidence.
        await using SqliteCommand journal = target.CreateCommand();
        journal.CommandText = "PRAGMA journal_mode=DELETE;";
        await journal.ExecuteNonQueryAsync();
    }

    await using BookstoreContext db = Open(copy, readOnly: true);
    List<AccessAuditEntry> rows = await db.AccessAuditEntries.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
    await File.WriteAllLinesAsync(export, rows.Select(r => JsonSerializer.Serialize(r, JsonSerializerOptions.Web)));

    string[] manifest = [.. new[] { copy, export }.Select(f => $"{Sha256(f)}  {Path.GetFileName(f)}")];
    await File.WriteAllLinesAsync(Path.Combine(evidenceDir, "manifest.sha256"), manifest);
    Console.WriteLine($"copied database, exported {rows.Count} audit rows");
    foreach (string line in manifest)
    {
        Console.WriteLine(line);
    }
    return 0;
}

static async Task<int> AffectedAsync(string dbPath, DateTime fromUtc, DateTime toUtc)
{
    await using BookstoreContext db = Open(dbPath, readOnly: true);
    IReadOnlyList<CrossInstitutionRead> reads = await IncidentQueries.CrossInstitutionReadsAsync(db, fromUtc, toUtc, CancellationToken.None);
    Console.WriteLine("victim  reader  reader-inst  requests  first (UTC)          last (UTC)           orders");
    foreach (CrossInstitutionRead r in reads)
    {
        Console.WriteLine($"{r.ResourceInstitutionId,6}  {r.UserId,-6}  {r.CallerInstitutionId,11}  {r.Requests,8}  " +
            $"{r.FirstAtUtc:yyyy-MM-dd HH:mm:ss}  {r.LastAtUtc:yyyy-MM-dd HH:mm:ss}  {string.Join(",", r.OrderIds)}");
    }
    Console.WriteLine($"affected institutions: {reads.Select(r => r.ResourceInstitutionId).Distinct().Count()}, " +
        $"affected orders: {reads.SelectMany(r => r.OrderIds).Distinct().Count()}");
    return 0;
}

static BookstoreContext Open(string dbPath, bool readOnly) =>
    new(new DbContextOptionsBuilder<BookstoreContext>()
        .UseSqlite($"Data Source={dbPath}" + (readOnly ? ";Mode=ReadOnly" : ""))
        .ConfigureWarnings(w => w.Ignore(SqliteEventId.SchemaConfiguredWarning))
        .Options);

static AccessAuditEntry Row(DateTime at, string user, int caller, int resource, string ids) =>
    new()
    {
        OccurredAtUtc = at, UserId = user, CallerInstitutionId = caller, Action = "institution.orders.list",
        ResourceInstitutionId = resource, ResourceIds = ids, Outcome = "allowed",
        TraceId = ActivityTraceId.CreateRandom().ToHexString()
    };

static string Sha256(string path)
{
    using FileStream stream = File.OpenRead(path);
    return Convert.ToHexStringLower(SHA256.HashData(stream));
}

static DateTime Utc(string value) =>
    DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

static int Usage()
{
    Console.Error.WriteLine("usage: demo-data <db> | preserve <db> <evidence-dir> | affected <db> <fromUtc> <toUtc>");
    return 2;
}
