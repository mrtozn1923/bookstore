using Bookstore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Bookstore.Api.Books;

public static class BookEndpoints
{
    private const int PageSize = 20;
    private const string BookListTag = "books";

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
            .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).SetVaryByQuery("page").Tag(BookListTag));

        // Price lookup for other services: one query for many books
        books.MapGet("/lookup", async ([FromQuery] string[] isbn, BookstoreContext db, CancellationToken ct) =>
            await db.Books.AsNoTracking()
                .Where(b => isbn.Contains(b.Isbn))
                .Select(b => new BookResponse(b.Isbn, b.Title, b.Author, b.Price, b.Currency))
                .ToListAsync(ct));

        // Back office: only store staff may add books
        books.MapPost("/", CreateBookAsync).RequireAuthorization("BackOffice");

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

    private static async Task<IResult> CreateBookAsync(
        CreateBookRequest request,
        BookstoreContext db,
        IOutputCacheStore cache,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (request.Isbn.Length != 13 || !request.Isbn.All(char.IsAsciiDigit))
        {
            return Results.Problem("ISBN must be 13 digits.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (await db.Books.AnyAsync(b => b.Isbn == request.Isbn, ct))
        {
            return Results.Conflict();
        }

        db.Books.Add(new Book { Isbn = request.Isbn, Title = request.Title, Author = request.Author, Price = request.Price });
        await db.SaveChangesAsync(ct);

        // A failed eviction must not fail the request; the list expires in 30 s anyway
        try
        {
            await cache.EvictByTagAsync(BookListTag, ct);
        }
        catch (Exception ex) when (ex is RedisException or AggregateException { InnerException: RedisException })
        {
            loggerFactory.CreateLogger("Bookstore.Api.Books").LogWarning(ex, "Could not evict the book list cache");
        }

        BookResponse created = new(request.Isbn, request.Title, request.Author, request.Price, "TRY");
        return Results.Created($"/api/books/{created.Isbn}", created);
    }
}
