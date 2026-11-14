using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Bookstore.PgLab.Catalog;

public static class CatalogDemo
{
    private static readonly string[] CategoryTags = ["classic", "sci-fi", "fantasy", "horror"];

    // copies 2,000 demo books from public.books into catalog.books through EF Core
    public static async Task SeedAsync(string connectionString)
    {
        await using BookstoreContext db = new(BookstoreContext.CreateOptions(connectionString));
        await db.Books.ExecuteDeleteAsync();

        await using NpgsqlConnection connection = new(connectionString);
        IEnumerable<(string Isbn, string Title, decimal Price, string Category)> source =
            await connection.QueryAsync<(string, string, decimal, string)>(
                "SELECT isbn, title, price, category FROM public.books WHERE book_id <= 2000 ORDER BY book_id");

        int i = 0;
        foreach ((string isbn, string title, decimal price, string category) in source)
        {
            i++;
            db.Books.Add(new Book
            {
                Isbn = isbn,
                Title = title,
                Price = price,
                Format = (BookFormat)(i % 4),
                Tags = [category.ToLowerInvariant() switch { "scifi" => "sci-fi", string c => c }, i % 5 == 0 ? "bestseller" : "backlist"],
                Details = new BookDetails { Pages = 120 + i * 7 % 900, Language = "en", FileFormats = i % 4 == 2 ? ["epub", "pdf"] : [] },
                CreatedAt = DateTime.UtcNow,
            });
        }
        int saved = await db.SaveChangesAsync();
        Book first = await db.Books.OrderBy(b => b.BookId).FirstAsync();
        Console.WriteLine($"saved {saved} books; first PublicId {first.PublicId} (version {first.PublicId.Version}), xmin {first.Version}");
    }

    public static async Task QueryAsync(string connectionString)
    {
        await using BookstoreContext db = new(BookstoreContext.CreateOptions(connectionString));

        // SQL Server's default collation is case-insensitive, PostgreSQL's comparison is not
        IQueryable<Book> exact = db.Books.Where(b => b.Title == "the hobbit #2");
        IQueryable<Book> ilike = db.Books.Where(b => EF.Functions.ILike(b.Title, "the hobbit #2"));
        Console.WriteLine($"== : {await exact.CountAsync()} rows");
        Console.WriteLine($"ILike : {await ilike.CountAsync()} rows");
        Console.WriteLine(ilike.ToQueryString());
        Console.WriteLine();

        IQueryable<Book> tagged = db.Books.Where(b => b.Tags.Contains("fantasy"));
        Console.WriteLine($"Tags.Contains: {await tagged.CountAsync()} rows");
        Console.WriteLine(tagged.ToQueryString());
        Console.WriteLine();

        IQueryable<Book> thick = db.Books.Where(b => b.Details.Pages > 1000 && b.Format == BookFormat.Ebook);
        Console.WriteLine($"Details.Pages: {await thick.CountAsync()} rows");
        Console.WriteLine(thick.ToQueryString());
        Console.WriteLine();

        IQueryable<Book> search = db.Books.Where(b => b.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", "hobbits")));
        Console.WriteLine($"full-text: {await search.CountAsync()} rows");
        Console.WriteLine(search.ToQueryString());
        Console.WriteLine();

        try
        {
            // raw SQL carried over from the SQL Server version of the project
            await db.Database.SqlQueryRaw<int>("SELECT TOP 5 [BookId] AS [Value] FROM [Books]").ToListAsync();
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"SQL Server syntax: {ex.SqlState} {ex.MessageText}");
        }

        int updated = await db.Books
            .Where(b => b.BookId == 1)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Details.Pages, 999));
        Console.WriteLine($"ExecuteUpdate on a JSON property: {updated} row");
    }

    public static async Task ConcurrencyAsync(string connectionString)
    {
        DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>(BookstoreContext.CreateOptions(connectionString))
            .LogTo(Console.WriteLine, [RelationalEventId.CommandExecuted])
            .Options;
        await using BookstoreContext selin = new(options);
        await using BookstoreContext emre = new(options);

        Book forSelin = await selin.Books.SingleAsync(b => b.BookId == 2);
        Book forEmre = await emre.Books.SingleAsync(b => b.BookId == 2);
        Console.WriteLine($"both loaded book 2 with xmin {forSelin.Version}");

        forSelin.Price = 29.90m;
        await selin.SaveChangesAsync();
        Console.WriteLine($"Selin saved, new xmin {forSelin.Version}");

        forEmre.Price = 27.50m;
        try
        {
            await emre.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine($"Emre: {ex.GetType().Name}: {ex.Message.Split('.')[0]}.");
        }
    }
}
