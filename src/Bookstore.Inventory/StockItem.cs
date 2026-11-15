using System.Linq.Expressions;

namespace Bookstore.Inventory;

public class StockItem
{
    public static readonly Expression<Func<StockItem, bool>> NeedsReorderExpression =
        i => i.Quantity < i.ReorderLevel;

    private static readonly Func<StockItem, bool> NeedsReorderRule = NeedsReorderExpression.Compile();

    public int Id { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public int ReorderLevel { get; set; } = 5;

    public bool NeedsReorder => NeedsReorderRule(this);
}
