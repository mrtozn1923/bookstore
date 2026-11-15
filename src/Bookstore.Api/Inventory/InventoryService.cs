using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory;

public sealed record StockLine(int BookId, int Quantity);

public sealed record ReservationResult(bool Succeeded, IReadOnlyList<int> UnavailableBookIds);

public interface IInventoryService
{
    Task<int> GetAvailableAsync(int bookId, CancellationToken ct);

    // Must run inside the caller's transaction: a failed reservation is undone by rolling it back.
    Task<ReservationResult> ReserveAsync(IReadOnlyCollection<StockLine> lines, CancellationToken ct);

    // New books start with no copies on hand until the first stock count.
    Task StartTrackingAsync(IReadOnlyCollection<int> bookIds, CancellationToken ct);
}

internal sealed class InventoryService(BookstoreContext db) : IInventoryService
{
    public async Task<int> GetAvailableAsync(int bookId, CancellationToken ct) =>
        await db.StockItems.AsNoTracking()
            .Where(s => s.BookId == bookId)
            .Select(s => s.QuantityOnHand - s.QuantityReserved)
            .FirstOrDefaultAsync(ct);

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

    public async Task StartTrackingAsync(IReadOnlyCollection<int> bookIds, CancellationToken ct)
    {
        db.StockItems.AddRange(bookIds.Select(id => new StockItem { BookId = id }));
        await db.SaveChangesAsync(ct);
    }
}

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services) =>
        services.AddScoped<IInventoryService, InventoryService>();
}
