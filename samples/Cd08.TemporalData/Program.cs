using System.Globalization;
using Bookstore.Institutional.Temporal;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;

await using (var setup = new BookstoreContext(options))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    await setup.Database.ExecuteSqlRawAsync(BookstoreContext.NoOverlapTrigger);
}

const int SampleSchool = 1;
await using var db = new BookstoreContext(options);
var service = new PricingService(db);

// January: card payment, 10 % institutional discount, open-ended
db.InstitutionPricing.Add(new InstitutionPricing
{
    InstitutionId = SampleSchool, PaymentTerm = "card", DiscountRate = 0.10m,
    ValidFrom = new DateOnly(2026, 1, 1), ValidTo = InstitutionPricing.OpenEnd
});
await db.SaveChangesAsync();
await Task.Delay(1000);

// the March invoice is issued with what we know now
DateTime marchInvoicedAt = DateTime.UtcNow;
await Task.Delay(1000);

// April 10: the renewed agreement (12 %) turns out to be valid from March 1
await service.ChangeAsync(SampleSchool, new DateOnly(2026, 3, 1), p => p.DiscountRate = 0.12m);
await Task.Delay(1000);
// May: the school moves to 30-day invoice payment
await service.ChangeAsync(SampleSchool, new DateOnly(2026, 5, 1), p => p.PaymentTerm = "invoice-30");

DateOnly boundary = new(2026, 3, 1);
int closed = await db.InstitutionPricing.CountAsync(p => p.InstitutionId == 1 && p.ValidFrom <= boundary && boundary <= p.ValidTo);
int halfOpen = await db.InstitutionPricing.CountAsync(p => p.InstitutionId == 1 && p.ValidFrom <= boundary && boundary < p.ValidTo);
Console.WriteLine($"Rows on {boundary:yyyy-MM-dd}: closed interval {closed}, half-open {halfOpen}");

Console.WriteLine($"March invoice issued at (UTC): {marchInvoicedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("Valid-time timeline (current knowledge):");
foreach (InstitutionPricing p in await db.InstitutionPricing.AsNoTracking().OrderBy(p => p.ValidFrom).ToListAsync())
{
    Console.WriteLine($"  [{p.ValidFrom:yyyy-MM-dd}, {p.ValidTo:yyyy-MM-dd})  {p.PaymentTerm,-11} {p.DiscountRate:P0}");
}

DateOnly mid = new(2026, 3, 15);
decimal now = await db.InstitutionPricing
    .Where(p => p.InstitutionId == 1 && p.ValidFrom <= mid && p.ValidTo > mid)
    .Select(p => p.DiscountRate)
    .SingleAsync();

IQueryable<decimal> thenQuery = db.InstitutionPricing
    .TemporalAsOf(marchInvoicedAt)
    .Where(p => p.InstitutionId == 1 && p.ValidFrom <= mid && p.ValidTo > mid)
    .Select(p => p.DiscountRate);
Console.WriteLine(thenQuery.ToQueryString());
decimal then = await thenQuery.SingleAsync();

decimal marchListTotal = 50_000m;
Console.WriteLine($"Discount valid on {mid:yyyy-MM-dd}, as we know it now:          {now:P0}");
Console.WriteLine($"Discount valid on {mid:yyyy-MM-dd}, as we knew it at invoicing: {then:P0}");
Console.WriteLine($"Credit due for March ({marchListTotal:N0} list price): {marchListTotal * (now - then):N0}");

Console.WriteLine("All versions (transaction time):");
var versions = await db.InstitutionPricing.TemporalAll()
    .OrderBy(p => EF.Property<DateTime>(p, "PeriodEnd")).ThenBy(p => p.Id)
    .Select(p => new
    {
        p.Id, p.ValidFrom, p.ValidTo, p.PaymentTerm, p.DiscountRate,
        From = EF.Property<DateTime>(p, "PeriodStart"),
        To = EF.Property<DateTime>(p, "PeriodEnd")
    })
    .ToListAsync();
foreach (var v in versions)
{
    string to = v.To.Year == 9999 ? "now" : v.To.ToString("HH:mm:ss");
    Console.WriteLine($"  #{v.Id} valid [{v.ValidFrom:MM-dd}, {v.ValidTo:yyyy-MM-dd}) {v.PaymentTerm,-11} {v.DiscountRate,4:P0}  recorded {v.From:HH:mm:ss} -> {to}");
}

try
{
    db.InstitutionPricing.Add(new InstitutionPricing
    {
        InstitutionId = SampleSchool, PaymentTerm = "card", DiscountRate = 0.15m,
        ValidFrom = new DateOnly(2026, 4, 15), ValidTo = new DateOnly(2026, 6, 1)
    });
    await db.SaveChangesAsync();
}
catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
{
    Console.WriteLine($"Rejected: {sql.Number} {sql.Message}");
}
