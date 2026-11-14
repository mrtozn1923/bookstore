using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory.Data;

// Sample stock for local development: 25 copies of every book from Catalog's sample data,
// except three that make the out-of-stock and last-copy paths easy to try.
public static class InventorySeed
{
    private static readonly string[] Isbns =
    [
        "9780441172719", "9780261102217", "9780141439587", "9780141439549", "9780141439518",
        "9780451524935", "9780060850524", "9780141439471", "9780142437247", "9780141441146",
        "9780141439556", "9780141439563", "9780143058144", "9780143035008", "9781400079988",
        "9780743273565", "9780553293357", "9780441478125", "9780140449266", "9780060934347"
    ];

    public static async Task SeedAsync(InventoryDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (await db.StockItems.AnyAsync(ct))
        {
            return;
        }
        DateTime now = clock.GetUtcNow().UtcDateTime;
        foreach (string isbn in Isbns)
        {
            db.StockItems.Add(new StockItem
            {
                Isbn = isbn,
                QuantityOnHand = isbn switch
                {
                    "9780060934347" => 0,  // Don Quixote
                    "9781400079988" => 3,  // War and Peace
                    "9780441478125" => 1,  // The Left Hand of Darkness: the last copy
                    _ => 25
                },
                UpdatedAtUtc = now
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
