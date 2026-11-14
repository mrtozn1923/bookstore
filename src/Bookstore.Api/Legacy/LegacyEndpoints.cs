using System.Text.Json;
using Bookstore.Api.Catalog;
using Bookstore.Api.Common;

namespace Bookstore.Api.Legacy;

// The old "v0" API kept as a counterexample: one endpoint, method name in the body,
// table columns in the response and every outcome as 200 OK.
public static class LegacyEndpoints
{
    private static readonly DateTimeOffset DeprecatedAt = new(2026, 11, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SunsetAt = new(2027, 3, 31, 23, 59, 59, TimeSpan.Zero);

    public sealed record RpcRequest(string Method, Dictionary<string, JsonElement>? Params);

    public static IEndpointRouteBuilder MapLegacyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/legacy/rpc", (RpcRequest request, CatalogData catalog, HttpResponse response) =>
        {
            // Deprecated since 2026-11-15, removed after 2027-03-31 (RFC 9745, RFC 8594).
            ApiVersions.WriteDeprecation(response, DeprecatedAt, SunsetAt, "https://api.example.com/docs/migrations/legacy-rpc");
            response.Headers.Append("Link", "<https://api.example.com/docs/sunset-policy>; rel=\"sunset\"; type=\"text/html\"");
            if (request.Method == "GetBooks")
            {
                try
                {
                    int page = int.Parse(request.Params?["page"].GetString() ?? "");
                    return Results.Ok(new { success = true, data = catalog.All.Skip((page - 1) * 10).Take(10) });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.ToString() });
                }
            }

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
                        AUTHOR_NAME = book.AuthorLine,
                        PRICE_TL = book.Price,
                        STOCK_QTY = book.StockQuantity,
                        SUPPLIER_CODE = book.Supplier,
                    },
                });
        });
        return app;
    }
}
