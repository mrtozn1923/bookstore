using Bookstore.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Endpoints;

public sealed record BookDto(int Id, string Isbn, string Title, decimal Price, int Stock);

public sealed record CreateBookRequest(string Isbn, string Title, decimal Price, int Stock);

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/api/books").WithTags("Books");

        books.MapGet("/", async (string? search, BookstoreDbContext db, CancellationToken ct) =>
        {
            IQueryable<Book> query = db.Books.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Title.Contains(search));
            }

            return await query
                .OrderBy(b => b.Title)
                .Select(b => new BookDto(b.Id, b.Isbn, b.Title, b.Price, b.Stock))
                .ToListAsync(ct);
        });

        books.MapGet("/{id:int}", async Task<Results<Ok<BookDto>, NotFound>> (int id, BookstoreDbContext db, CancellationToken ct) =>
        {
            BookDto? book = await db.Books
                .Where(b => b.Id == id)
                .Select(b => new BookDto(b.Id, b.Isbn, b.Title, b.Price, b.Stock))
                .SingleOrDefaultAsync(ct);
            return book is null ? TypedResults.NotFound() : TypedResults.Ok(book);
        });

        books.MapPost("/", async Task<Results<Created<BookDto>, ValidationProblem, Conflict<string>>> (
            CreateBookRequest request, BookstoreDbContext db, CancellationToken ct) =>
        {
            Dictionary<string, string[]> errors = [];
            if (request.Price <= 0)
            {
                errors["price"] = ["Price must be greater than zero."];
            }
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                errors["title"] = ["Title is required."];
            }
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }
            if (await db.Books.AnyAsync(b => b.Isbn == request.Isbn, ct))
            {
                return TypedResults.Conflict($"A book with ISBN {request.Isbn} already exists.");
            }

            Book book = new() { Isbn = request.Isbn, Title = request.Title, Price = request.Price, Stock = request.Stock };
            db.Books.Add(book);
            await db.SaveChangesAsync(ct);

            BookDto dto = new(book.Id, book.Isbn, book.Title, book.Price, book.Stock);
            return TypedResults.Created($"/api/books/{book.Id}", dto);
        }).RequireAuthorization("CatalogAdmin");

        // Old catalog links use the ISBN; redirect them to the canonical URL
        app.MapGet("/books/isbn/{isbn}", async Task<Results<RedirectHttpResult, NotFound>> (string isbn, BookstoreDbContext db, CancellationToken ct) =>
        {
            int? id = await db.Books.Where(b => b.Isbn == isbn).Select(b => (int?)b.Id).SingleOrDefaultAsync(ct);
            return id is null ? TypedResults.NotFound() : TypedResults.Redirect($"/api/books/{id}", permanent: true);
        }).ExcludeFromDescription();

        return app;
    }
}
