using System.Net;
using Bookstore.Institutional.MultiTenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

BookstoreContext NewContext(int tenantId, bool sessionContext)
{
    ITenantContext tenant = new FixedTenant(tenantId);
    DbContextOptionsBuilder<BookstoreContext> b = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(connectionString);
    if (sessionContext) b.AddInterceptors(new TenantSessionContextInterceptor(tenant));
    return new BookstoreContext(b.Options, tenant);
}

// Setup: 1 = Sample School, 2 = Sample Library; Grace works for both
DateTimeOffset t0 = new(2026, 4, 20, 9, 0, 0, TimeSpan.Zero);
await using (BookstoreContext setup = NewContext(1, sessionContext: false))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    setup.InstitutionUsers.AddRange(
        new InstitutionUser { FullName = "Ada Lovelace", Email = "ada@example.com" },
        new InstitutionUser { FullName = "Alan Turing", Email = "alan@example.com" },
        new InstitutionUser { FullName = "Grace Hopper", Email = "grace@example.com" });
    setup.PurchaseApprovals.AddRange(
        new PurchaseApproval { OrderId = 101, RequestedById = 1, ApproverId = 2, Status = ApprovalStatus.Pending, RequestedAt = t0 },
        new PurchaseApproval { OrderId = 102, RequestedById = 1, ApproverId = 2, Status = ApprovalStatus.Approved, RequestedAt = t0, DecidedAt = t0.AddHours(2) },
        new PurchaseApproval { OrderId = 103, RequestedById = 3, ApproverId = 2, Status = ApprovalStatus.Pending, RequestedAt = t0 });
    setup.Invoices.Add(new Invoice { InvoiceNumber = "INV-2026-0042", TotalAmount = 4800m });
    await setup.SaveChangesAsync();
}
await using (BookstoreContext setup = NewContext(2, sessionContext: false))
{
    setup.InstitutionUsers.AddRange(
        new InstitutionUser { FullName = "Linus Torvalds", Email = "linus@example.com" },
        new InstitutionUser { FullName = "Grace Hopper", Email = "grace@example.com" });
    setup.PurchaseApprovals.Add(
        new PurchaseApproval { OrderId = 201, RequestedById = 4, ApproverId = 5, Status = ApprovalStatus.Pending, RequestedAt = t0 });
    setup.Invoices.Add(new Invoice { InvoiceNumber = "INV-2026-0043", TotalAmount = 12500m });
    await setup.SaveChangesAsync();
}

// 1) Naive: every query writes its own filter
await using (NaiveContext naiveDb = new(new DbContextOptionsBuilder<NaiveContext>().UseSqlServer(connectionString).Options))
{
    InstitutionUserReports reports = new(naiveDb);
    Console.WriteLine($"naive list(1): {string.Join(", ", await reports.ListAsync(1))}");
    Console.WriteLine($"naive count(1): {await reports.CountAsync(1)}");
}

// 2) Query filter only: raw SQL skips it
await using (BookstoreContext school = NewContext(1, sessionContext: false))
{
    List<string> raw = await school.Database
        .SqlQueryRaw<string>("SELECT Email AS Value FROM dbo.InstitutionUsers")
        .ToListAsync();
    Console.WriteLine($"raw SQL, filter only: {string.Join(", ", raw)}");
}

// 3) Database per tenant: only the connection string changes
Dictionary<int, string> databases = new() { [1] = "Bookstore_sample_school", [2] = "Bookstore_sample_library" };
foreach (int tenantId in databases.Keys)
{
    SqlConnectionStringBuilder csb = new("Server=localhost;Trusted_Connection=True;TrustServerCertificate=True")
    {
        InitialCatalog = databases[tenantId]
    };
    Console.WriteLine($"tenant {tenantId} -> {csb.InitialCatalog}");
}

// 4) Row-level security as the second lock
await using (BookstoreContext admin = NewContext(1, sessionContext: false))
{
    string script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "cd13-02-rls-policy.sql"));
    foreach (string batch in script.Split("\nGO", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (batch.Length > 0) await admin.Database.ExecuteSqlRawAsync(batch);
    }
}

// 5) The API: tenant from the subdomain
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5313");
builder.Services.AddSingleton<ITenantCatalog, InMemoryTenantCatalog>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<TenantSessionContextInterceptor>();
builder.Services.AddDbContext<BookstoreContext>((sp, o) => o
    .UseSqlServer(connectionString)
    .AddInterceptors(sp.GetRequiredService<TenantSessionContextInterceptor>()));

WebApplication app = builder.Build();
app.UseMiddleware<TenantResolutionMiddleware>();
app.MapGet("/api/institution-users", async (BookstoreContext db, CancellationToken ct) =>
    await db.InstitutionUsers.OrderBy(u => u.Id).Select(u => new { u.Id, u.Email }).ToListAsync(ct));
await app.StartAsync();

using (HttpClient http = new())
{
    foreach (string host in new[] { "sample-school.bookstore.test", "sample-library.bookstore.test", "unknown.bookstore.test" })
    {
        using HttpRequestMessage req = new(HttpMethod.Get, "http://127.0.0.1:5313/api/institution-users");
        req.Headers.Host = host;
        using HttpResponseMessage res = await http.SendAsync(req);
        Console.WriteLine($"{host} -> {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
    }
}

await using (BookstoreContext school = NewContext(1, sessionContext: true))
{
    List<string> raw = await school.Database
        .SqlQueryRaw<string>("SELECT Email AS Value FROM dbo.InstitutionUsers")
        .ToListAsync();
    Console.WriteLine($"raw SQL, school session: {string.Join(", ", raw)}");
    Console.WriteLine($"IgnoreQueryFilters count (RLS on): {await school.InstitutionUsers.IgnoreQueryFilters().CountAsync()}");
}
await using (BookstoreContext noSession = NewContext(1, sessionContext: false))
{
    Console.WriteLine($"IgnoreQueryFilters count (no session context): {await noSession.InstitutionUsers.IgnoreQueryFilters().CountAsync()}");
    Console.WriteLine($"filtered count (no session context): {await noSession.InstitutionUsers.CountAsync()}");
}
await using (BookstoreContext school = NewContext(1, sessionContext: true))
{
    try
    {
        await school.Database.ExecuteSqlRawAsync(
            "INSERT dbo.InstitutionUsers (InstitutionId, FullName, Email) VALUES (2, N'Edsger Dijkstra', 'edsger@example.com')");
    }
    catch (SqlException ex)
    {
        Console.WriteLine($"SqlException {ex.Number}: {ex.Message}");
    }
}

// 6) Connection pool: the same session, a different tenant each time
foreach (int tenantId in new[] { 1, 2, 1 })
{
    await using BookstoreContext db = NewContext(tenantId, sessionContext: true);
    int spid = await db.Database.SqlQueryRaw<int>("SELECT CAST(@@SPID AS int) AS Value").SingleAsync();
    int users = await db.InstitutionUsers.CountAsync();
    Console.WriteLine($"tenant {tenantId}: spid {spid}, institution users {users}");
}

// 7) Background job: one scope per tenant
await NightlyApprovalReminderJob.RunAsync(app.Services.GetRequiredService<IServiceScopeFactory>(), [1, 2]);
try
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    await db.PurchaseApprovals.CountAsync();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"job without tenant: {ex.Message}");
}
await app.StopAsync();

// 8) Isolation checks: the model and the database
await using (BookstoreContext db = NewContext(1, sessionContext: false))
{
    foreach (IEntityType et in db.Model.GetEntityTypes().Where(t => typeof(ITenantOwned).IsAssignableFrom(t.ClrType)))
    {
        bool ok = et.FindDeclaredQueryFilter(BookstoreContext.TenantFilter) is not null;
        Console.WriteLine($"{et.ClrType.Name,-16} tenant filter: {(ok ? "OK" : "MISSING")}");
    }
    // Same query as sql/cd13-03-unprotected-tables.sql
    List<string> unprotected = await db.Database.SqlQuery<string>($"""
        SELECT t.name AS Value
        FROM sys.tables AS t
        JOIN sys.columns AS c ON c.object_id = t.object_id AND c.name = N'InstitutionId'
        WHERE NOT EXISTS (
            SELECT 1 FROM sys.security_predicates AS p
            WHERE p.target_object_id = t.object_id AND p.predicate_type_desc = N'FILTER')
        """).ToListAsync();
    Console.WriteLine($"UnprotectedTable: {string.Join(", ", unprotected)}");
}
