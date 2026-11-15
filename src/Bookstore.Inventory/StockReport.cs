using System.Text;

namespace Bookstore.Inventory;

public class StockReport
{
    public string Build(IReadOnlyList<StockItem> items)
    {
        decimal totalValue = items.Sum(i => i.Quantity * i.UnitCost);
        List<StockItem> reorder = items.Where(i => i.NeedsReorder).ToList();
        Dictionary<string, int> perWarehouse = items
            .GroupBy(i => i.Warehouse)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        StringBuilder sb = new();
        sb.AppendLine(FormattableString.Invariant($"Stock value: {totalValue:0.00}"));
        foreach (string warehouse in perWarehouse.Keys.Order())
            sb.AppendLine($"{warehouse}: {perWarehouse[warehouse]} copies");
        sb.AppendLine("Reorder:");
        foreach (StockItem item in reorder)
            sb.AppendLine("! " + FormatLine(item));
        return sb.ToString();
    }

    public string ReorderEmailBody(IReadOnlyList<StockItem> items)
    {
        StringBuilder sb = new();
        sb.AppendLine("Please reorder:");
        foreach (StockItem item in items.Where(i => i.NeedsReorder))
            sb.AppendLine("! " + FormatLine(item));
        return sb.ToString();
    }

    public string PickList(IReadOnlyList<StockItem> items, string warehouse)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Pick list for {warehouse}:");
        foreach (StockItem item in items.Where(i => i.Warehouse == warehouse))
            sb.AppendLine(FormatLine(item));
        return sb.ToString();
    }

    private static string FormatLine(StockItem item) =>
        $"{item.Isbn} {item.Title} ({item.Warehouse}): {item.Quantity}";
}
