using Bookstore.Inventory;

namespace Bookstore.Inventory.Tests;

// Runs once before all fixtures in this namespace; holds read-only data only
[SetUpFixture]
public class InventoryTestRun
{
    public static IReadOnlyList<StockItem> LargeWarehouse { get; private set; } = [];

    [OneTimeSetUp]
    public void BuildLargeWarehouse() =>
        LargeWarehouse =
        [
            .. Enumerable.Range(0, 10_000)
                .Select(i => new StockItem($"978{i:D10}", i % 2 == 0 ? "Istanbul" : "Ankara", OnHand: i % 50, Reserved: 0)),
        ];
}
