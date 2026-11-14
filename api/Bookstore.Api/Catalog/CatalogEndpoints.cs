namespace Bookstore.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/api/books");

        books.MapGet("/", (BookCatalog catalog, int? take) =>
            TypedResults.Ok(catalog.All.Take(Math.Clamp(take ?? BookCatalog.CatalogSize, 1, BookCatalog.CatalogSize))));

        books.MapGet("/featured", (BookCatalog catalog) => TypedResults.Ok(catalog.Featured));

        books.MapGet("/new", (BookCatalog catalog) => TypedResults.Ok(catalog.NewArrivals));

        books.MapGet("/{isbn}", Results<Ok<Book>, NotFound> (string isbn, BookCatalog catalog) =>
            catalog.ByIsbn.TryGetValue(isbn, out Book? book) ? TypedResults.Ok(book) : TypedResults.NotFound());

        return app;
    }
}
