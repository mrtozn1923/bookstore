using Bookstore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Books;

public static class BookEndpoints
{
    private const int PageSize = 20;

    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/api/books");

        // The catalog is large: the list is always paged
        books.MapGet("/", async (BookstoreContext db, CancellationToken ct, int page = 1) =>
                await db.Books.AsNoTracking()
                    .OrderBy(b => b.Title)
                    .Skip((Math.Max(page, 1) - 1) * PageSize)
                    .Take(PageSize)
                    .Select(b => new BookResponse(b.Isbn, b.Title, b.Author, b.Price, b.Currency))
                    .ToListAsync(ct))
            .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).SetVaryByQuery("page"));

        // Price lookup for other services: one query for many books
        books.MapGet("/lookup", async ([FromQuery] string[] isbn, BookstoreContext db, CancellationToken ct) =>
            await db.Books.AsNoTracking()
                .Where(b => isbn.Contains(b.Isbn))
                .Select(b => new BookResponse(b.Isbn, b.Title, b.Author, b.Price, b.Currency))
                .ToListAsync(ct));

        books.MapGet("/{isbn}", async (string isbn, BookstoreContext db, CancellationToken ct) =>
        {
            BookResponse? book = await db.Books.AsNoTracking()
                .Where(b => b.Isbn == isbn)
                .Select(b => new BookResponse(b.Isbn, b.Title, b.Author, b.Price, b.Currency))
                .FirstOrDefaultAsync(ct);
            if (book is null)
            {
                return Results.NotFound();
            }

            // For the book page: up to three other titles by the same author
            string[] moreByAuthor = await db.Books.AsNoTracking()
                .Where(b => b.Author == book.Author && b.Isbn != isbn)
                .OrderBy(b => b.Title)
                .Select(b => b.Title)
                .Take(3)
                .ToArrayAsync(ct);

            return Results.Ok(new BookDetailResponse(
                book.Isbn, book.Title, book.Author, book.Price, book.Currency, moreByAuthor));
        });

        return app;
    }
}
