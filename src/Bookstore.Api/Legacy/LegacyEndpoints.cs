using System.Text.Json;
using Bookstore.Api.Catalog;

namespace Bookstore.Api.Legacy;

// The old "v0" API kept as a counterexample: one endpoint, method name in the body,
// table columns in the response and every outcome as 200 OK.
public static class LegacyEndpoints
{
    public sealed record RpcRequest(string Method, Dictionary<string, JsonElement>? Params);

    public static IEndpointRouteBuilder MapLegacyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/legacy/rpc", (RpcRequest request, CatalogData catalog) =>
        {
            if (request.Method != "GetBook")
            {
                return Results.Ok(new { success = false, error = $"Unknown method {request.Method}" });
            }

            string isbn = request.Params?["isbn"].GetString() ?? "";
            Book? book = catalog.Find(isbn);
            return book is null
                ? Results.Ok(new { success = false, error = "Book not found" })
                : Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        BOOK_ISBN = book.Isbn,
                        BOOK_TITLE = book.Title,
                        AUTHOR_NAME = book.Author,
                        PRICE_TL = book.Price,
                        STOCK_QTY = book.StockQuantity,
                        SUPPLIER_CODE = book.Supplier,
                    },
                });
        });
        return app;
    }
}
