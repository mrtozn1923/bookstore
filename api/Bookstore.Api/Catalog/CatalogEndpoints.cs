using Bookstore.Api.Lab;

namespace Bookstore.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/api/books").AddEndpointFilter<LabDelayFilter>();

        books.MapGet("/", (BookCatalog catalog, int? take) =>
            TypedResults.Ok(catalog.All.Take(Math.Clamp(take ?? BookCatalog.CatalogSize, 1, BookCatalog.CatalogSize))));

        books.MapGet("/featured", (BookCatalog catalog) => TypedResults.Ok(catalog.Featured));

        books.MapGet("/new", (BookCatalog catalog) => TypedResults.Ok(catalog.NewArrivals));

        books.MapGet("/{isbn}", Results<Ok<Book>, NotFound> (string isbn, BookCatalog catalog) =>
            catalog.ByIsbn.TryGetValue(isbn, out Book? book) ? TypedResults.Ok(book) : TypedResults.NotFound());

        books.MapGet("/{isbn}/similar", Results<Ok<IEnumerable<Book>>, NotFound> (string isbn, int? count, BookCatalog catalog) =>
            catalog.ByIsbn.TryGetValue(isbn, out Book? book)
                ? TypedResults.Ok(catalog.Similar(book, Math.Clamp(count ?? 4, 1, 8)))
                : TypedResults.NotFound());

        return app;
    }
}
