using System.Buffers.Text;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Catalog;

public static class BookEndpoints
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/books");
        books.MapGet("", ListBooks).WithName("listBooks");
        books.MapGet("/{isbn}", GetBook).WithName("getBook");
        return app;
    }

    private static Results<Ok<BookListResponse>, ProblemHttpResult> ListBooks(
        CatalogData catalog, int? pageSize, string? pageToken)
    {
        int size = Math.Min(pageSize ?? DefaultPageSize, MaxPageSize);
        if (size < 1)
        {
            return TypedResults.Problem(statusCode: 400, title: "Invalid page size",
                detail: $"pageSize must be between 1 and {MaxPageSize}.");
        }

        // First version of the page token: base64url("offset=N"). Post 05 replaces it.
        int offset = 0;
        if (pageToken is not null)
        {
            string decoded = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(pageToken));
            offset = int.Parse(decoded["offset=".Length..]);
        }

        List<BookResponse> items = [.. catalog.All.Skip(offset).Take(size).Select(BookResponse.From)];
        string? next = offset + size < catalog.All.Count
            ? Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"offset={offset + size}"))
            : null;
        return TypedResults.Ok(new BookListResponse(items, next));
    }

    private static Results<Ok<BookResponse>, ProblemHttpResult> GetBook(string isbn, CatalogData catalog) =>
        catalog.Find(isbn) is { } book
            ? TypedResults.Ok(BookResponse.From(book))
            : TypedResults.Problem(statusCode: 404, title: "Book not found",
                detail: $"No book with ISBN {isbn} exists in the catalog.");
}
