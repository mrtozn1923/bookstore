using System.Globalization;
using Bookstore.Institutional.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

var services = new ServiceCollection();
services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 4, 14, 9, 30, 0, TimeSpan.Zero)));
services.AddScoped<AuditContext>();
services.AddScoped<AuditInterceptor>();
services.AddScoped<InvoiceMaintenance>();
services.AddDbContext<BookstoreContext>((sp, options) => options
    .UseSqlServer(cs)
    .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
await using ServiceProvider root = services.BuildServiceProvider();

await using (AsyncServiceScope setup = root.CreateAsyncScope())
{
    BookstoreContext db = setup.ServiceProvider.GetRequiredService<BookstoreContext>();
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
}

// every block below is a separate request (its own scope, its own AuditContext)
async Task RequestAsync(string user, string correlationId, string? reason, Func<IServiceProvider, BookstoreContext, Task> work)
{
    await using AsyncServiceScope scope = root.CreateAsyncScope();
    AuditContext audit = scope.ServiceProvider.GetRequiredService<AuditContext>();
    audit.UserName = user;
    audit.CorrelationId = correlationId;
    audit.Reason = reason;
    await work(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<BookstoreContext>());
}

await RequestAsync("alan", "c7f1-01", null, async (_, db) =>
{
    db.Invoices.Add(new Invoice { InstitutionId = 1, InvoiceNumber = "INV-2026-0042", TotalAmount = 48000m, DueDate = new DateOnly(2026, 5, 15) });
    await db.SaveChangesAsync();
});
await RequestAsync("grace", "c7f1-02", null, async (_, db) =>
{
    db.InstitutionUsers.Add(new InstitutionUser { InstitutionId = 1, FullName = "Ada Lovelace", Phone = "+90 555 000 00 01" });
    await db.SaveChangesAsync();
});
await RequestAsync("alan", "c7f1-03", "Credit note CN-118 for returned books", async (_, db) =>
{
    Invoice invoice = await db.Invoices.SingleAsync(i => i.InvoiceNumber == "INV-2026-0042");
    invoice.TotalAmount = 4800m;
    await db.SaveChangesAsync();
});
await RequestAsync("grace", "c7f1-04", "User request SUP-5512", async (_, db) =>
{
    InstitutionUser ada = await db.InstitutionUsers.SingleAsync(u => u.FullName == "Ada Lovelace");
    ada.Phone = "+90 555 000 00 02";
    await db.SaveChangesAsync();
});

// the measurement promised in the soft delete post
await RequestAsync("alan", "c7f1-05", null, async (_, db) =>
{
    int before = await db.AuditEntries.CountAsync();
    int updated = await db.Invoices
        .Where(i => i.DueDate < new DateOnly(2026, 6, 1))
        .ExecuteUpdateAsync(s => s.SetProperty(i => i.DueDate, i => i.DueDate.AddDays(15)));
    int after = await db.AuditEntries.CountAsync();
    Console.WriteLine($"ExecuteUpdate changed {updated} invoice(s); audit entries: {before} -> {after}");
});

await RequestAsync("alan", "c7f1-06", "Bank holiday, Q2 due dates shifted", async (sp, _) =>
{
    await sp.GetRequiredService<InvoiceMaintenance>().PostponeDueDatesAsync(new DateOnly(2026, 7, 1), 7);
});

await RequestAsync("system", "c7f1-07", null, async (_, db) =>
{
    foreach (AuditEntry e in await db.AuditEntries.OrderBy(a => a.Id).ToListAsync())
    {
        Console.WriteLine($"{e.ChangedAt:yyyy-MM-dd HH:mm} {e.UserName,-6} {e.Action,-8} {e.EntityType}#{e.EntityId} reason={e.Reason ?? "-"} corr={e.CorrelationId}");
        Console.WriteLine($"    {e.Changes}");
    }
    Invoice invoice = await db.Invoices.AsNoTracking().SingleAsync();
    Console.WriteLine($"Invoice now: {invoice.InvoiceNumber} {invoice.TotalAmount} due {invoice.DueDate:yyyy-MM-dd}");
});
