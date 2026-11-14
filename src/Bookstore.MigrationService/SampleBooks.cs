using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.MigrationService;

public static class SampleBooks
{
    private static readonly Book[] books =
    [
        new() { Isbn = "9780441172719", Title = "Dune", Author = "Frank Herbert", Price = 420.00m },
        new() { Isbn = "9780141439587", Title = "Emma", Author = "Jane Austen", Price = 260.00m },
        new() { Isbn = "9780547928227", Title = "The Hobbit", Author = "J. R. R. Tolkien", Price = 340.00m },
        new() { Isbn = "9780141439549", Title = "Middlemarch", Author = "George Eliot", Price = 380.00m },
        new() { Isbn = "9780060934347", Title = "Don Quixote", Author = "Miguel de Cervantes", Price = 450.00m },
    ];

    public static async Task<int> SeedAsync(BookstoreContext db, int generatedBooks, CancellationToken ct)
    {
        if (await db.Books.AnyAsync(ct))
        {
            return 0;
        }

        db.Books.AddRange(books);
        int added = await db.SaveChangesAsync(ct);

        if (generatedBooks > 0)
        {
            // Development only: a large catalog makes slow queries visible in the dashboard
            added += await db.Database.ExecuteSqlAsync($"""
                WITH numbers AS (
                    SELECT TOP ({generatedBooks}) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
                    FROM sys.all_objects a CROSS JOIN sys.all_objects b)
                INSERT INTO Books (Isbn, Title, Author, Price, Currency)
                SELECT '979' + RIGHT('0000000000' + CAST(n AS varchar(10)), 10),
                       CONCAT('Generated Book ', n), CONCAT('Author ', n % 2000), 100 + n % 400, 'TRY'
                FROM numbers
                """, ct);
        }

        return added;
    }
}
