using System.ComponentModel.DataAnnotations;
using Bookstore.Auth;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

public sealed record SetPriceRequest([property: Range(typeof(decimal), "1", "10000")] decimal Price);

public static class PriceEndpoints
{
    public static IEndpointRouteBuilder MapPriceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/books/{isbn}/price", SetPrice)
            .WithTags("Catalog")
            .RequireAuthorization(ApiPolicies.ManageCatalog);
        return app;
    }

    // PUT /api/books/{isbn}/price { "price": 349.00 }: only store staff and admins change prices.
    private static async Task<Results<NoContent, NotFound>> SetPrice(
        string isbn, SetPriceRequest request, BookstoreContext db, ILoggerFactory loggers, HttpContext http, CancellationToken ct)
    {
        int updated = await db.Books
            .Where(b => b.Isbn == isbn)
            .ExecuteUpdateAsync(b => b.SetProperty(x => x.Price, request.Price), ct);
        if (updated == 0)
        {
            return TypedResults.NotFound();
        }

        loggers.CreateLogger("Bookstore.Catalog.Prices")
            .LogInformation("Price of {Isbn} set to {Price} by {User}", isbn, request.Price, http.User.Identity?.Name);
        return TypedResults.NoContent();
    }
}
