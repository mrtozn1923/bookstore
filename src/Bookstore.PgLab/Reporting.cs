using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bookstore.PgLab.Reporting;

public sealed class Book
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
}

public sealed class Order
{
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public short Quantity { get; set; }
}

// post 21: a row of the mv_top_books materialized view (keyless, read-only)
public sealed class TopBook
{
    public long Rank { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public long Units { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class ReportingContext(DbContextOptions<ReportingContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<TopBook> TopBooks => Set<TopBook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // post 21: views and materialized views are mapped with ToView and have no key
        modelBuilder.Entity<TopBook>().HasNoKey().ToView("mv_top_books");

        // snake_case table and column names (post 12 does this with EFCore.NamingConventions)
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            string? tableName = entity.GetTableName();
            if (tableName is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }
            foreach (IMutableProperty property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string name) => Regex.Replace(name, "(?<=[a-z0-9])([A-Z])", "_$1").ToLowerInvariant();
}

public static class Bestsellers
{
    public static async Task RunAsync(string connectionString, int repeat)
    {
        DbContextOptions<ReportingContext> options = new DbContextOptionsBuilder<ReportingContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using ReportingContext db = new(options);

        DateTime since = new(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        string category = "Fantasy";

        var query = db.OrderItems
            .TagWith("bestsellers-by-category")
            .Where(i => i.Order.CreatedAt >= since && i.Order.Status != "Cancelled" && i.Book.Category == category)
            .GroupBy(i => new { i.BookId, i.Book.Title })
            .Select(g => new { g.Key.BookId, g.Key.Title, Sold = g.Sum(i => i.Quantity) })
            .OrderByDescending(b => b.Sold)
            .Take(10);

        Console.WriteLine(query.ToQueryString());

        for (int i = 1; i < repeat; i++)
        {
            await query.ToListAsync();
        }
        var top = await query.ToListAsync();
        Console.WriteLine();
        foreach (var b in top.Take(3))
        {
            Console.WriteLine($"{b.BookId,6} {b.Title,-22} {b.Sold,4}");
        }
    }
}

public static class TopBooksReport
{
    public static async Task RunAsync(string connectionString)
    {
        DbContextOptions<ReportingContext> options = new DbContextOptionsBuilder<ReportingContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using ReportingContext db = new(options);

        IQueryable<TopBook> query = db.TopBooks
            .Where(t => t.Category == "Fantasy")
            .OrderBy(t => t.Rank)
            .Take(3);

        Console.WriteLine(query.ToQueryString());
        Console.WriteLine();
        foreach (TopBook book in await query.ToListAsync())
        {
            Console.WriteLine(FormattableString.Invariant($"{book.Rank,3} {book.BookId,6} {book.Title,-20} {book.Units,4} {book.Revenue,9:F2}"));
        }

        TopBook first = await db.TopBooks.OrderBy(t => t.Rank).FirstAsync();
        Console.WriteLine($"tracked: {db.ChangeTracker.Entries().Count()}");
        first.Units = 0;
        Console.WriteLine($"saved: {await db.SaveChangesAsync()}");
    }
}
