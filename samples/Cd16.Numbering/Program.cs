using System.Diagnostics;
using Bookstore.Billing.Numbering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(connectionString).Options;
BookstoreContext NewContext() => new(options);

await using (BookstoreContext setup = NewContext())
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    Console.WriteLine(setup.Database.GenerateCreateScript().Split('\n').First(l => l.Contains("CREATE UNIQUE INDEX")).Trim());
    await setup.Database.ExecuteSqlRawAsync(
        "CREATE TABLE NaiveInvoices (Id int IDENTITY PRIMARY KEY, Seq int NOT NULL CONSTRAINT UQ_NaiveInvoices_Seq UNIQUE);" +
        "CREATE SEQUENCE dbo.InvoiceSeq AS int START WITH 1 INCREMENT BY 1 CACHE 50;");
}

// 1) MAX + 1 under 50 concurrent requests
int inserted = 0, duplicates = 0;
string? firstError = null;
await Task.WhenAll(Enumerable.Range(0, 50).Select(async _ =>
{
    await using BookstoreContext db = NewContext();
    try
    {
        await db.Database.ExecuteSqlRawAsync(
            "DECLARE @next int = (SELECT ISNULL(MAX(Seq), 0) + 1 FROM NaiveInvoices); INSERT NaiveInvoices (Seq) VALUES (@next);");
        Interlocked.Increment(ref inserted);
    }
    catch (SqlException ex) when (ex.Number == 2627)
    {
        Interlocked.Increment(ref duplicates);
        Interlocked.CompareExchange(ref firstError, $"SqlException {ex.Number}: {ex.Message.Split('\n')[0]}", null);
    }
}));
Console.WriteLine(firstError);
Console.WriteLine($"MAX+1: {inserted} inserted, {duplicates} duplicate key errors");

// 2) The allocator refuses to work outside a transaction
await using (BookstoreContext db = NewContext())
{
    try
    {
        await new InvoiceNumberAllocator(db).NextAsync("BKS", 2026, default);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
Console.WriteLine($"{InvoiceNumber.Format("BKS", 2026, 42)} ({InvoiceNumber.Format("BKS", 2026, 42).Length} chars)");

async Task<int[]> CreateDraftsAsync(int count, DateOnly date)
{
    await using BookstoreContext db = NewContext();
    List<Invoice> drafts = Enumerable.Range(0, count)
        .Select(i => new Invoice { InstitutionId = 1 + i % 3, InvoiceDate = date, TotalAmount = 1000m + i, Status = InvoiceStatus.Draft })
        .ToList();
    db.Invoices.AddRange(drafts);
    await db.SaveChangesAsync();
    return drafts.Select(d => d.Id).ToArray();
}

// 3) Month end: 50 concurrent approvals, every tenth one fails before commit
DateOnly november = new(2026, 11, 30);
int[] ids = await CreateDraftsAsync(50, november);
List<string> approved = [];
await Task.WhenAll(ids.Select(async (id, i) =>
{
    await using BookstoreContext db = NewContext();
    InvoiceApprovalService svc = new(db, new InvoiceNumberAllocator(db));
    try
    {
        string number = await svc.ApproveAsync(id, beforeCommit: i % 10 == 9 ? () => throw new InvalidOperationException("signing failed") : null);
        lock (approved) approved.Add(number);
    }
    catch (InvalidOperationException) { }
}));
List<int> seqs = approved.Select(n => int.Parse(n[7..])).Order().ToList();
Console.WriteLine($"counter: approved {seqs.Count}, min {seqs.Min()}, max {seqs.Max()}, distinct {seqs.Distinct().Count()}, gaps {seqs.Max() - seqs.Min() + 1 - seqs.Count}");
Console.WriteLine($"first numbers: {string.Join(", ", approved.Order().Take(3))}");

// 4) The same scenario with a SEQUENCE
List<int> committed = [];
await Task.WhenAll(Enumerable.Range(0, 50).Select(async i =>
{
    await using BookstoreContext db = NewContext();
    await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync();
    int value = (await db.Database.SqlQuery<int>($"SELECT NEXT VALUE FOR dbo.InvoiceSeq AS Value").ToListAsync()).Single();
    if (i % 10 == 9)
    {
        await tx.RollbackAsync();
        return;
    }
    await tx.CommitAsync();
    lock (committed) committed.Add(value);
}));
int max = committed.Max();
Console.WriteLine($"sequence: committed {committed.Count}, max {max}, missing {string.Join(",", Enumerable.Range(1, max).Except(committed))}");

// 5) Throughput: where the slow work sits relative to the number
async Task<long> MeasureAsync(bool numberFirst)
{
    int[] drafts = await CreateDraftsAsync(50, november);
    Stopwatch sw = Stopwatch.StartNew();
    await Task.WhenAll(drafts.Select(async id =>
    {
        await using BookstoreContext db = NewContext();
        InvoiceApprovalService svc = new(db, new InvoiceNumberAllocator(db));
        Func<Task> work = () => Task.Delay(20);
        await (numberFirst ? svc.ApproveAsync(id, beforeCommit: work) : svc.ApproveAsync(id, beforeNumber: work));
    }));
    return sw.ElapsedMilliseconds;
}
await MeasureAsync(numberFirst: false); // warm-up
Console.WriteLine($"number first, 20 ms work inside lock  : 50 approvals in {await MeasureAsync(numberFirst: true)} ms");
Console.WriteLine($"20 ms work first, number last         : 50 approvals in {await MeasureAsync(numberFirst: false)} ms");

// 6) Year change: the year comes from the invoice date
foreach (DateOnly date in new[] { new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 2) })
{
    int id = (await CreateDraftsAsync(1, date)).Single();
    await using BookstoreContext db = NewContext();
    string number = await new InvoiceApprovalService(db, new InvoiceNumberAllocator(db)).ApproveAsync(id);
    Console.WriteLine($"dated {date:yyyy-MM-dd} -> {number}");
}

// 7) A cancelled invoice keeps its number
await using (BookstoreContext db = NewContext())
{
    string first = approved.Order().First();
    Invoice cancelled = await db.Invoices.SingleAsync(i => i.InvoiceNumber == first);
    cancelled.Status = InvoiceStatus.Cancelled;
    await db.SaveChangesAsync();
    db.Invoices.Add(new Invoice { InstitutionId = 1, InvoiceDate = november, TotalAmount = 10m, Status = InvoiceStatus.Approved, InvoiceNumber = first });
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
    {
        Console.WriteLine(sql.Message.Split('\n')[0]);
    }
}
