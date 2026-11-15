namespace Bookstore.ML.Lab;

/// <summary>Post 01: the delivery estimate a developer would write by hand, measured against real outcomes.</summary>
public static class RuleBaseline
{
    public static double EstimateDays(OrderRow o)
    {
        double days = o.DistanceKm switch
        {
            < 100 => 1,
            < 500 => 2,
            < 1000 => 3,
            _ => 4,
        };
        if (o.ShippingMethod == "Express") days -= 1;
        if (o.HasBackorderItem) days += 5;
        return Math.Max(1, days);
    }

    public static void Run()
    {
        List<OrderRow> delivered = [.. OrderRow.Load().Where(o => o.DeliveryDays > 0)];

        double[] errors = [.. delivered.Select(o => Math.Abs(EstimateDays(o) - o.DeliveryDays!.Value))];
        Console.WriteLine($"Delivered orders     : {delivered.Count:N0}");
        Console.WriteLine($"Mean absolute error  : {errors.Average():F2} days");
        Console.WriteLine($"Within +/- 1 day     : {errors.Count(e => e <= 1) / (double)errors.Length:P1}");
        Console.WriteLine($"Off by 3+ days       : {errors.Count(e => e >= 3) / (double)errors.Length:P1}");

        Console.WriteLine();
        Console.WriteLine("Where the rule is wrong the most (mean error by group):");
        var worst = delivered
            .GroupBy(o => (o.Carrier, Weekend: o.OrderedAtUtc.AddHours(3).DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday))
            .Select(g => (g.Key, Error: g.Average(o => EstimateDays(o) - o.DeliveryDays!.Value), Count: g.Count()))
            .OrderBy(x => x.Error);
        foreach (((string carrier, bool weekend), double error, int count) in worst)
        {
            Console.WriteLine($"  {carrier,-9} ordered Fri/Sat={weekend,-5}  bias {error,6:F2} days  ({count:N0} orders)");
        }
    }
}
