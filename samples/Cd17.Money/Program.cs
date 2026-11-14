using System.Globalization;
using System.Text.Json;
using Bookstore.Billing.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

// 1) The opening: 10% institutional discount on 112.25 TL
decimal discount = 112.25m * 0.10m;
Console.WriteLine($"Math.Round default      : {Math.Round(discount, 2)}");
Console.WriteLine($"MidpointRounding.AwayFromZero: {Math.Round(discount, 2, MidpointRounding.AwayFromZero)}");

// 2) double vs decimal
double d = 0;
for (int i = 0; i < 10; i++) d += 0.1;
decimal m = 0;
for (int i = 0; i < 10; i++) m += 0.1m;
Console.WriteLine($"double: {d:R} == 1.0 ? {d == 1.0}");
Console.WriteLine($"decimal: {m} == 1.0m ? {m == 1.0m}");

// 3) Naive division and Allocate
decimal third = Math.Round(100m / 3, 2);
Console.WriteLine($"naive 100/3   : {third:F2} TRY, {third:F2} TRY, {third:F2} TRY");
Console.WriteLine($"100 TRY / 3   : {string.Join(", ", Money.Try(100m).Allocate(1, 1, 1))}");
Console.WriteLine($"0.05 TRY 70/30: {string.Join(", ", Money.Try(0.05m).Allocate(70, 30))}");
try
{
    Money sum = Money.Try(10m) + new Money(10m, Currency.EUR);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

// 4) VAT per line vs from total: three 33.33 TL shipping lines at 20%
InvoiceLine[] lines = [new(Money.Try(33.33m), 0.20m), new(Money.Try(33.33m), 0.20m), new(Money.Try(33.33m), 0.20m)];
Money[] perLine = Vat.PerLine(lines), fromTotal = Vat.FromTotal(lines);
Console.WriteLine($"per line : {string.Join(" + ", perLine)} = {perLine.Aggregate((a, b) => a + b)}");
Console.WriteLine($"from total: {string.Join(" + ", fromTotal)} = {fromTotal.Aggregate((a, b) => a + b)}");

// 5) Rates by date
VatRateTable table = new(
[
    new(VatCategory.General, new DateOnly(2007, 12, 30), new DateOnly(2023, 7, 10), 0.18m),
    new(VatCategory.General, new DateOnly(2023, 7, 10), null, 0.20m),
    new(VatCategory.Reduced, new DateOnly(2007, 12, 30), new DateOnly(2023, 7, 10), 0.08m),
    new(VatCategory.Reduced, new DateOnly(2023, 7, 10), null, 0.10m),
    new(VatCategory.Basic, new DateOnly(2007, 12, 30), null, 0.01m),
]);
DateOnly today = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
foreach (DateOnly date in new[] { new DateOnly(2023, 7, 9), new DateOnly(2023, 7, 10), today })
{
    Console.WriteLine($"{date:yyyy-MM-dd}: general {table.For(VatCategory.General, date)}, reduced {table.For(VatCategory.Reduced, date)}, basic {table.For(VatCategory.Basic, date)}");
}

// 6) EF Core: Money as a complex type, and a decimal without precision
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>()
    .UseSqlServer(connectionString)
    .LogTo(msg => Console.WriteLine(msg[(msg.IndexOf("No store type", StringComparison.Ordinal))..].Trim()),
        (id, level) => id == Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.DecimalTypeDefaultWarning)
    .Options;
await using (BookstoreContext db = new(options))
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    string script = db.Database.GenerateCreateScript();
    Console.WriteLine(script[script.IndexOf("CREATE TABLE", StringComparison.Ordinal)..(script.IndexOf(");", StringComparison.Ordinal) + 2)]);
    db.Invoices.Add(new BillingInvoice { LegacyAmount = 6.666m, Total = Money.Try(120m) });
    await db.SaveChangesAsync();
}
await using (BookstoreContext db = new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(connectionString).Options))
{
    BillingInvoice saved = await db.Invoices.AsNoTracking().SingleAsync();
    Console.WriteLine($"LegacyAmount read back: {saved.LegacyAmount}");
    IQueryable<BillingInvoice> q = db.Invoices.Where(i => i.Total.Currency == Currency.TRY && i.Total.Amount > 100m);
    Console.WriteLine(q.ToQueryString());
    Console.WriteLine($"matches: {await q.CountAsync()}");
}

// 7) JSON: amounts as strings
Console.WriteLine($"0.1 + 0.2 (double) = {0.1 + 0.2:R}");
Console.WriteLine($"as JavaScript number: {(double)12345678901234567.89m:R}");
JsonSerializerOptions json = new(JsonSerializerDefaults.Web) { Converters = { new MoneyJsonConverter() } };
Console.WriteLine(JsonSerializer.Serialize(new { total = Money.Try(12345678901234567.89m), rawTotal = 12345678901234567.89m }, json));
Money back = JsonSerializer.Deserialize<Money>(JsonSerializer.Serialize(Money.Try(99.99m), json), json);
Console.WriteLine($"round trip: {back}");
