using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory;

public sealed record StockLine(int BookId, int Quantity);

public sealed record ReservationResult(bool Succeeded, IReadOnlyList<int> UnavailableBookIds);

public interface IInventoryService
{
    Task<int> GetAvailableAsync(int bookId, CancellationToken ct);

    // Available copies of many books in one query (bookId → available); unknown books are left out.
    Task<IReadOnlyDictionary<int, int>> GetAvailableAsync(IReadOnlyCollection<int> bookIds, CancellationToken ct);

    // Must run inside the caller's transaction: a failed reservation is undone by rolling it back.
    Task<ReservationResult> ReserveAsync(IReadOnlyCollection<StockLine> lines, CancellationToken ct);

    // Gives reserved copies back, e.g. when the payment of an order fails.
    Task ReleaseAsync(IReadOnlyCollection<StockLine> lines, CancellationToken ct);

    // Back office: new copies arrive at the warehouse.
    Task<bool> ReceiveAsync(int bookId, int quantity, CancellationToken ct);
}

internal sealed class InventoryService(BookstoreContext db) : IInventoryService
{
    public async Task<int> GetAvailableAsync(int bookId, CancellationToken ct) =>
        await db.StockItems.AsNoTracking()
            .Where(s => s.BookId == bookId)
            .Select(s => s.QuantityOnHand - s.QuantityReserved)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> GetAvailableAsync(IReadOnlyCollection<int> bookIds, CancellationToken ct) =>
        await db.StockItems.AsNoTracking()
            .Where(s => bookIds.Contains(s.BookId))
            .ToDictionaryAsync(s => s.BookId, s => s.QuantityOnHand - s.QuantityReserved, ct);

    public async Task<ReservationResult> ReserveAsync(IReadOnlyCollection<StockLine> lines, CancellationToken ct)
    {
        List<int> unavailable = [];

        // One conditional UPDATE per book, always in BookId order, so concurrent orders lock rows in the same order.
        foreach (StockLine line in lines.OrderBy(l => l.BookId))
        {
            int updated = await db.StockItems
                .Where(s => s.BookId == line.BookId && s.QuantityOnHand - s.QuantityReserved >= line.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityReserved, x => x.QuantityReserved + line.Quantity), ct);
            if (updated == 0)
            {
                unavailable.Add(line.BookId);
            }
        }

        return new ReservationResult(unavailable.Count == 0, unavailable);
    }

    public async Task ReleaseAsync(IReadOnlyCollection<StockLine> lines, CancellationToken ct)
    {
        foreach (StockLine line in lines.OrderBy(l => l.BookId))
        {
            await db.StockItems
                .Where(s => s.BookId == line.BookId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityReserved, x => x.QuantityReserved - line.Quantity), ct);
        }
    }

    public async Task<bool> ReceiveAsync(int bookId, int quantity, CancellationToken ct) =>
        await db.StockItems
            .Where(s => s.BookId == bookId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuantityOnHand, x => x.QuantityOnHand + quantity), ct) == 1;
}

public sealed record StockReceipt(string Isbn, [property: System.ComponentModel.DataAnnotations.Range(1, 10_000)] int Quantity);

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services) =>
        services.AddScoped<IInventoryService, InventoryService>()
            .AddHostedService<InventoryMetrics>();

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/inventory/receipts  { "isbn": "...", "quantity": 50 }
        app.MapPost("/api/inventory/receipts", async Task<Results<NoContent, NotFound>> (
            StockReceipt receipt, Catalog.ICatalogService catalog, IInventoryService inventory, CancellationToken ct) =>
        {
            Catalog.BookInfo? book = await catalog.FindByIsbnAsync(receipt.Isbn, ct);
            return book is not null && await inventory.ReceiveAsync(book.Id, receipt.Quantity, ct)
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        }).WithTags("Inventory");
        return app;
    }
}
