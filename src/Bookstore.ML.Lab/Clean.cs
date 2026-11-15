using System.Globalization;

namespace Bookstore.ML.Lab;

/// <summary>Post 02: fix what the quality report found and write data/orders_clean.csv.</summary>
public static class Clean
{
    public static void Run()
    {
        string header = File.ReadLines("data/orders.csv").First();
        List<OrderRow> rows = [.. OrderRow.Load()];
        int before = rows.Count;

        // 1) The export wrote some orders twice: keep one line per OrderId
        rows = [.. rows.DistinctBy(r => r.OrderId)];
        int afterDedup = rows.Count;

        // 2) One spelling per city: the most common form of the trimmed, case-insensitive name
        Dictionary<string, string> canonical = rows
            .GroupBy(r => r.City.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.GroupBy(r => r.City).OrderByDescending(x => x.Count()).First().Key);
        int cityFixes = rows.Count(r => r.City != canonical[r.City.Trim().ToUpperInvariant()]);
        rows = [.. rows.Select(r => r with { City = canonical[r.City.Trim().ToUpperInvariant()] })];

        // 3) Totals stored in kuruş: compare with the order lines and divide by 100
        Dictionary<string, decimal> itemTotals = Csv.Read("data/order_items.csv")
            .GroupBy(f => f[0])
            .ToDictionary(g => g.Key, g => g.Sum(f => int.Parse(f[2], CultureInfo.InvariantCulture)
                                                      * decimal.Parse(f[3], CultureInfo.InvariantCulture)));
        int totalFixes = rows.Count(r => r.OrderTotal > 10 * itemTotals[r.OrderId]);
        rows = [.. rows.Select(r => r.OrderTotal > 10 * itemTotals[r.OrderId] ? r with { OrderTotal = r.OrderTotal / 100 } : r)];

        // 4) A delivery cannot take negative time: the label is wrong, drop the row
        int badLabels = rows.Count(r => r.DeliveryDays <= 0);
        rows = [.. rows.Where(r => r.DeliveryDays is null || r.DeliveryDays > 0)];

        File.WriteAllLines("data/orders_clean.csv", [header, .. rows.Select(ToCsv)]);
        Console.WriteLine($"rows {before:N0} -> {afterDedup:N0} after de-duplication -> {rows.Count:N0} written");
        Console.WriteLine($"city spellings fixed: {cityFixes}, distinct cities: {rows.Select(r => r.City).Distinct().Count()}");
        Console.WriteLine($"totals divided by 100: {totalFixes}, rows dropped for a negative label: {badLabels}");
        Console.WriteLine($"missing weight kept as empty: {rows.Count(r => r.TotalWeightKg is null)}");
    }

    private static string ToCsv(OrderRow r)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        static string Iso(DateTime d) => d.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";
        return string.Join(',', r.OrderId, r.CustomerId, Iso(r.OrderedAtUtc), r.City, r.District, r.Warehouse,
            r.DistanceKm.ToString(inv), r.Carrier, r.ShippingMethod, r.ItemCount.ToString(inv),
            r.TotalWeightKg?.ToString("0.00", inv) ?? "", r.OrderTotal.ToString("0.00", inv),
            r.DiscountRate.ToString("0.00", inv), B(r.IsCampaign), r.PaymentMethod, B(r.HasBackorderItem),
            r.CustomerNote, B(r.IsCancelled), r.DeliveryDays?.ToString("0.00", inv) ?? "",
            r.TrackingEventCount?.ToString(inv) ?? "", B(r.IsReturned),
            r.ReturnRequestedUtc is DateTime d ? Iso(d) : "");
    }
}
