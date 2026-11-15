using Bookstore.Catalog;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api;

public static class BookEndpoints
{
    public static RouteGroupBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/books")
            .WithTags("Book");

        group.MapGet("/", GetAll);
        group.MapGet("/{isbn}", GetByIsbn);
        return group;
    }

    private static Ok<IReadOnlyList<Book>> GetAll(IBookCatalog catalog)
        => TypedResults.Ok(catalog.GetAll());

    private static Results<Ok<Book>, NotFound> GetByIsbn(string isbn, IBookCatalog catalog)
        => catalog.FindByIsbn(isbn) is { } book ? TypedResults.Ok(book) : TypedResults.NotFound();
}
