namespace Bookstore.Inventory;

public sealed record StockItem(string Isbn, string Warehouse, int OnHand, int Reserved)
{
    public int Available => OnHand - Reserved;
}

// Decides which titles to reorder from how many days the available stock will last
public sealed class ReorderPolicy(double minimumDaysOfCover)
{
    public double MinimumDaysOfCover { get; } = minimumDaysOfCover;

    public static double DaysOfCover(int available, double averageDailySales)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(averageDailySales);
        return averageDailySales == 0 ? double.PositiveInfinity : available / averageDailySales;
    }

    public IReadOnlyList<string> TitlesToReorder(
        IEnumerable<StockItem> items, IReadOnlyDictionary<string, double> averageDailySales) =>
    [
        .. items
            .GroupBy(i => i.Isbn)
            .Where(g => DaysOfCover(g.Sum(i => i.Available), averageDailySales.GetValueOrDefault(g.Key)) < MinimumDaysOfCover)
            .Select(g => g.Key)
            .Order(),
    ];
}
