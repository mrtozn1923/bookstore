using Bookstore.Api.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Catalog;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/books");
        books.MapGet("", ListBooks).WithName("listBooks");
        books.MapGet("/{isbn}", GetBook).WithName("getBook");
        return app;
    }

    private static Results<Ok<Page<BookResponse>>, ProblemHttpResult> ListBooks(
        CatalogData catalog, int? pageSize, string? pageToken) =>
        Paging.Paginate([.. catalog.All.Select(BookResponse.From)], pageSize, pageToken);

    private static Results<Ok<BookResponse>, ProblemHttpResult> GetBook(string isbn, CatalogData catalog) =>
        catalog.Find(isbn) is { } book
            ? TypedResults.Ok(BookResponse.From(book))
            : TypedResults.Problem(statusCode: 404, title: "Book not found",
                detail: $"No book with ISBN {isbn} exists in the catalog.");
}
