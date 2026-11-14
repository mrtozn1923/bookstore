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

    public static async Task<int> SeedAsync(BookstoreContext db, CancellationToken ct)
    {
        if (await db.Books.AnyAsync(ct))
        {
            return 0;
        }

        db.Books.AddRange(books);
        return await db.SaveChangesAsync(ct);
    }
}
