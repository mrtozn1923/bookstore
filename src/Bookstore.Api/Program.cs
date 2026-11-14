using Bookstore.Data;
using Bookstore.Reporting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReportingContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Reporting"))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddDbContext<BookstoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Bookstore")));

var app = builder.Build();

app.MapGet("/books/search", async (string q, BookstoreContext db) =>
    await db.Books
        .Where(b => b.Title.Contains(q) || b.Author.Name.Contains(q))
        .OrderBy(b => b.Title)
        .Select(b => new { b.Id, b.Title, Author = b.Author.Name, b.Price })
        .Take(20)
        .ToListAsync());

app.MapGet("/books/bestsellers", async (int? days, BookstoreContext db) =>
{
    var since = DateTime.UtcNow.AddDays(-(days ?? 30));
    return await db.BestSellers(since)
        .Join(db.Books, s => s.BookId, b => b.Id, (s, b) => new { b.Title, s.Units })
        .OrderByDescending(x => x.Units)
        .Take(5)
        .ToListAsync();
});

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

app.Run();
