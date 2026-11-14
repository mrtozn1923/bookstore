using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Books;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/api/books");

        books.MapGet("/", async (BookstoreContext db, CancellationToken ct) =>
                await db.Books.AsNoTracking()
                    .OrderBy(b => b.Title)
                    .Select(b => new BookResponse(b.Isbn, b.Title, b.Author, b.Price, b.Currency))
                    .ToListAsync(ct))
            .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)));

        books.MapGet("/{isbn}", async (string isbn, BookstoreContext db, CancellationToken ct) =>
            await db.Books.AsNoTracking()
                .Where(b => b.Isbn == isbn)
                .Select(b => new BookResponse(b.Isbn, b.Title, b.Author, b.Price, b.Currency))
                .FirstOrDefaultAsync(ct) is BookResponse book
                ? Results.Ok(book)
                : Results.NotFound());

        return app;
    }
}
