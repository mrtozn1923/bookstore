using System.Globalization;
using Microsoft.Data.Analysis;

namespace Bookstore.ML.Lab;

/// <summary>Post 02: look at the data before any model sees it.</summary>
public static class Explore
{
    public static void Run()
    {
        DataFrame df = DataFrame.LoadCsv("data/orders.csv");
        Console.WriteLine($"{df.Rows.Count:N0} rows, {df.Columns.Count} columns");
        DataFrame some = new(
            df.Columns["OrderedAtUtc"], df.Columns["City"], df.Columns["DistanceKm"], df.Columns["TotalWeightKg"],
            df.Columns["CustomerNote"], df.Columns["DeliveryDays"], df.Columns["ReturnRequestedUtc"]);
        Console.WriteLine(some.Info());

        DataFrame numeric = new(
            df.Columns["DistanceKm"], df.Columns["ItemCount"], df.Columns["TotalWeightKg"],
            df.Columns["OrderTotal"], df.Columns["DeliveryDays"]);
        Console.WriteLine(numeric.Description());

        // Description() divides by all rows, nulls included; compare with a LINQ average over non-null values
        PrimitiveDataFrameColumn<float> label = (PrimitiveDataFrameColumn<float>)df.Columns["DeliveryDays"];
        Console.WriteLine($"DeliveryDays mean over non-null values: {label.Where(v => v.HasValue).Average(v => v!.Value):F4}");
    }

    public static void Quality()
    {
        string[] lines = File.ReadAllLines("data/orders.csv")[1..];
        List<OrderRow> rows = [.. lines.Select(l => OrderRow.Parse(Csv.Split(l)))];

        // 1) Duplicates: the same OrderId twice, and whether the whole line is identical
        var duplicateIds = rows.GroupBy(r => r.OrderId).Where(g => g.Count() > 1).ToList();
        int identical = lines.GroupBy(l => l).Sum(g => g.Count() - 1);
        Console.WriteLine($"Duplicate OrderIds   : {duplicateIds.Count} (identical lines: {identical})");

        // 2) Missing values
        Console.WriteLine($"Missing weight       : {rows.Count(r => r.TotalWeightKg is null)}");
        Console.WriteLine($"Missing label        : {rows.Count(r => r.DeliveryDays is null)} " +
                          $"(cancelled: {rows.Count(r => r.IsCancelled)})");

        // 3) Inconsistent categories
        var cities = rows.GroupBy(r => r.City).Select(g => (Name: g.Key, Count: g.Count())).ToList();
        var odd = cities.Where(c => c.Name != c.Name.Trim() || c.Name == c.Name.ToUpperInvariant()
                                    || c.Name == c.Name.ToLowerInvariant());
        Console.WriteLine($"Distinct City values : {cities.Count}; odd spellings: " +
                          string.Join(", ", odd.OrderByDescending(c => c.Count).Take(6).Select(c => $"'{c.Name}' x{c.Count}")));

        // 4) Impossible or extreme values
        List<double> days = [.. rows.Where(r => r.DeliveryDays is not null).Select(r => r.DeliveryDays!.Value).Order()];
        double q1 = Quantile(days, 0.25), q3 = Quantile(days, 0.75), iqr = q3 - q1;
        Console.WriteLine($"DeliveryDays <= 0    : {days.Count(d => d <= 0)}");
        Console.WriteLine($"DeliveryDays p50/p90/p99/max : {Quantile(days, 0.5):F2} / {Quantile(days, 0.9):F2} / " +
                          $"{Quantile(days, 0.99):F2} / {days[^1]:F2}");
        Console.WriteLine($"IQR outliers (> {q3 + 1.5 * iqr:F2}) : {days.Count(d => d > q3 + 1.5 * iqr)}; " +
                          $"> 15 days: {days.Count(d => d > 15)}");

        Dictionary<string, decimal> itemTotals = Csv.Read("data/order_items.csv")
            .GroupBy(f => f[0])
            .ToDictionary(g => g.Key, g => g.Sum(f => int.Parse(f[2], CultureInfo.InvariantCulture)
                                                      * decimal.Parse(f[3], CultureInfo.InvariantCulture)));
        var suspicious = rows.DistinctBy(r => r.OrderId).Where(r => r.OrderTotal > 10 * itemTotals[r.OrderId]).ToList();
        Console.WriteLine($"OrderTotal > 10x items subtotal : {suspicious.Count} " +
                          $"(e.g. {suspicious[0].OrderId}: {suspicious[0].OrderTotal:N2} vs {itemTotals[suspicious[0].OrderId]:N2})");

        // 5) Class balance for the yes/no columns
        int n = rows.Count;
        Console.WriteLine($"IsCancelled = true   : {rows.Count(r => r.IsCancelled) / (double)n:P1}");
        Console.WriteLine($"IsReturned = true    : {rows.Count(r => r.IsReturned) / (double)rows.Count(r => !r.IsCancelled):P1} of delivered");

        // 6) Labels can be wrong too: reviews about delivery that were tagged as something else
        List<string[]> reviews = [.. Csv.Read("data/reviews.csv")];
        Console.WriteLine("Review topics        : " + string.Join(", ", reviews.GroupBy(r => r[5])
            .OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}")));
        string[] shippingWords = ["The package", "The courier", "Shipping was", "Fast delivery", "Delivered on time"];
        foreach (string[] r in reviews.Where(r => r[5] == "PrintQuality" && shippingWords.Any(r[4].StartsWith)).DistinctBy(r => r[4]).Take(3))
        {
            Console.WriteLine($"  {r[0]} [{r[5]}] {r[4]}");
        }

        // Histogram for the figure
        Directory.CreateDirectory("out");
        File.WriteAllLines("out/02-delivery-histogram.csv",
            ["bin,count", .. days.Where(d => d > 0).GroupBy(d => Math.Min(20, (int)Math.Floor(d * 2) / 2.0))
                .OrderBy(g => g.Key).Select(g => $"{g.Key.ToString(CultureInfo.InvariantCulture)},{g.Count()}")]);
    }

    public static double Quantile(IReadOnlyList<double> sorted, double q)
    {
        double pos = (sorted.Count - 1) * q;
        int lo = (int)Math.Floor(pos);
        int hi = Math.Min(lo + 1, sorted.Count - 1);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
    }
}
