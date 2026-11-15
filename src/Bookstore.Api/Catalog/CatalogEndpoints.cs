namespace Bookstore.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/books/{isbn}/summary", async Task<IResult> (
            string isbn, IBookRepository books, ClaudeCatalogService catalog, CancellationToken ct) =>
        {
            Book? book = books.FindByIsbn(isbn);
            if (book is null)
            {
                return TypedResults.NotFound();
            }

            BookSummary? summary = await catalog.SummarizeAsync(book, ct);
            return summary is null
                ? TypedResults.Problem("The summary service is not available right now.", statusCode: StatusCodes.Status503ServiceUnavailable)
                : TypedResults.Ok(summary);
        });

        return app;
    }
}
