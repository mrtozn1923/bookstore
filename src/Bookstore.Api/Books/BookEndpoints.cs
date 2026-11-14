namespace Bookstore.Api.Books;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/api/books");

        books.MapGet("/", (BookCatalog catalog) => catalog.GetAll());

        books.MapGet("/{isbn}", (string isbn, BookCatalog catalog) =>
            catalog.Find(isbn) is Book book ? Results.Ok(book) : Results.NotFound());

        return app;
    }
}
