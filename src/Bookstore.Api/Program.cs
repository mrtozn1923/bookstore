using Bookstore.Reporting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReportingContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Reporting"))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

var app = builder.Build();

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
