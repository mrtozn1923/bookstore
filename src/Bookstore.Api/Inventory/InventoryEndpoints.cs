using System.ComponentModel.DataAnnotations;
using Bookstore.Auth;
using Bookstore.Catalog;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory;

public sealed record StockView(string Isbn, string Title, int OnHand, int Reserved, int Available);

public sealed record SetStockRequest([property: Range(0, 10_000)] int QuantityOnHand);

// Back-office endpoints used by Bookstore.Admin: any staff member may read and count stock.
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder inventory = app.MapGroup("/api/inventory").WithTags("Inventory")
            .RequireAuthorization(ApiPolicies.Staff);
        inventory.MapGet("/", GetStock);
        inventory.MapPut("/{isbn}", SetQuantityOnHand);
        return app;
    }

    // GET /api/inventory: every book with its stock, the scarcest first.
    private static async Task<Ok<List<StockView>>> GetStock(BookstoreContext db, ICatalogService catalog, CancellationToken ct)
    {
        List<StockItem> items = await db.StockItems.AsNoTracking().ToListAsync(ct);
        IReadOnlyDictionary<int, BookInfo> books = await catalog.GetByIdsAsync([.. items.Select(s => s.BookId)], ct);

        List<StockView> stock = [.. items
            .Where(s => books.ContainsKey(s.BookId))
            .Select(s => new StockView(books[s.BookId].Isbn, books[s.BookId].Title, s.QuantityOnHand, s.QuantityReserved, s.Available))
            .OrderBy(s => s.Available)
            .ThenBy(s => s.Title)];
        return TypedResults.Ok(stock);
    }

    // PUT /api/inventory/{isbn} { "quantityOnHand": 30 }: a stock count from the warehouse.
    private static async Task<Results<NoContent, NotFound, ValidationProblem>> SetQuantityOnHand(
        string isbn, SetStockRequest request, BookstoreContext db, ICatalogService catalog, CancellationToken ct)
    {
        BookInfo? book = await catalog.FindByIsbnAsync(isbn, ct);
        if (book is null)
        {
            return TypedResults.NotFound();
        }

        int updated = await db.StockItems
            .Where(s => s.BookId == book.Id && s.QuantityReserved <= request.QuantityOnHand)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityOnHand, request.QuantityOnHand), ct);
        if (updated == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["QuantityOnHand"] = ["Quantity on hand cannot be lower than the reserved quantity."]
            });
        }

        return TypedResults.NoContent();
    }
}
