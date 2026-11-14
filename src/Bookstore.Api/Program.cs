using System.Diagnostics;
using Bookstore.Api.Diagnostics;
using Bookstore.Api.Orders;
using Bookstore.Data;
using Bookstore.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services.AddDbContext<ReportingContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Reporting"))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddDbContext<BookstoreContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Bookstore"), sql =>
    {
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        sql.CommandTimeout(15);
    });
    options.ConfigureWarnings(w => w.Throw(CoreEventId.RowLimitingOperationWithoutOrderByWarning));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
    }
});

if (builder.Configuration.GetValue<bool>("Telemetry:Console"))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("Bookstore.Api"))
        .WithTracing(t => t.AddAspNetCoreInstrumentation().AddEntityFrameworkCoreInstrumentation().AddConsoleExporter())
        .WithMetrics(m => m.AddMeter("Microsoft.EntityFrameworkCore").AddConsoleExporter());
}

var app = builder.Build();

var slowCommandThreshold = TimeSpan.FromMilliseconds(app.Configuration.GetValue("Diagnostics:SlowCommandMs", 200));
DiagnosticListener.AllListeners.Subscribe(
    new SlowCommandObserver(app.Services.GetRequiredService<ILogger<SlowCommandObserver>>(), slowCommandThreshold));

app.MapGet("/books/search", async (string q, BookstoreContext db) =>
    await db.Books
        .TagWithCallSite()
        .Where(b => b.Title.Contains(q) || b.Author.Name.Contains(q))
        .OrderBy(b => b.Title)
        .Select(b => new { b.Id, b.Title, Author = b.Author.Name, b.Price })
        .Take(20)
        .ToListAsync());

app.MapGet("/books/bestsellers", async (int? days, BookstoreContext db) =>
{
    var since = DateTime.UtcNow.AddDays(-(days ?? 30));
    return await db.BestSellers(since)
        .TagWith("GET /books/bestsellers")
        .Join(db.Books, s => s.BookId, b => b.Id, (s, b) => new { b.Title, s.Units })
        .OrderByDescending(x => x.Units)
        .Take(5)
        .ToListAsync();
});

app.MapGet("/books/any", async (BookstoreContext db) =>
    await db.Books.Select(b => b.Title).Take(1).ToListAsync());

app.MapGet("/reports/daily-sales", async (ReportingContext db) =>
    await db.VwDailySales
        .OrderBy(s => s.SaleDate)
        .ThenBy(s => s.CategoryName)
        .ToListAsync());

app.MapGet("/reports/top-books", async (ReportingContext db) =>
    await db.BookSales
        .GroupBy(s => s.BookTitle)
        .Select(g => new { Title = g.Key, Units = g.Sum(s => s.Quantity) })
        .OrderByDescending(x => x.Units)
        .Take(3)
        .ToListAsync());

app.MapOrderEndpoints();

app.Run();
