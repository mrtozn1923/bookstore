using Algorithms.Intervals;

namespace Algorithms.Demos.Demos;

public static class IntervalDemos
{
    public static void Campaigns()
    {
        // As typed in the back office form: "first day" and "last day", both inclusive
        (string Name, DateOnly First, DateOnly Last)[] typed =
        [
            ("Autumn Classics", new(2026, 11, 1), new(2026, 11, 10)),
            ("Black Friday", new(2026, 11, 10), new(2026, 11, 20)),
            ("Winter Reads", new(2026, 11, 21), new(2026, 12, 5)),
        ];

        DateOnly day = new(2026, 11, 10);
        Interval<DateOnly>[] wrong = [.. typed.Select(c => new Interval<DateOnly>(c.First, c.Last))];
        Console.WriteLine($"End = last day:     Autumn Classics active on {day:yyyy-MM-dd}? {wrong[0].Contains(day)}, overlap: {Describe(IntervalOps.FindOverlap(wrong))}");

        Interval<DateOnly>[] halfOpen = [.. typed.Select(c => new Interval<DateOnly>(c.First, c.Last.AddDays(1)))];
        Console.WriteLine($"End = last day + 1: Autumn Classics active on {day:yyyy-MM-dd}? {halfOpen[0].Contains(day)}, overlap: {Describe(IntervalOps.FindOverlap(halfOpen))}");
    }

    private static string Describe((Interval<DateOnly> First, Interval<DateOnly> Second)? clash) =>
        clash is null ? "none" : $"{Format(clash.Value.First)} and {Format(clash.Value.Second)}";

    private static string Format(Interval<DateOnly> i) => $"[{i.Start:yyyy-MM-dd}, {i.End:yyyy-MM-dd})";

    public static void DeliverySlots()
    {
        TimeSpan tr = TimeSpan.FromHours(3);
        DateTimeOffset At(int h, int m) => new(2026, 11, 16, h, m, 0, tr);
        Interval<DateTimeOffset>[] bookings =
        [
            new(At(9, 0), At(11, 0)), new(At(9, 30), At(10, 30)), new(At(10, 0), At(12, 0)),
            new(At(10, 30), At(11, 30)), new(At(11, 0), At(13, 0)), new(At(14, 0), At(16, 0)),
        ];
        (int max, DateTimeOffset at) = IntervalOps.MaxConcurrent(bookings);
        Console.WriteLine($"vans needed: {max} (from {at:HH:mm})");
    }

    public static void Shifts()
    {
        TimeOnly T(int h) => new(h, 0);
        Interval<TimeOnly>[] shifts = [new(T(6), T(14)), new(T(13), T(20)), new(T(8), T(12))];
        Interval<TimeOnly> openHours = new(T(6), T(22));
        foreach (Interval<TimeOnly> gap in IntervalOps.Gaps(openHours, shifts))
            Console.WriteLine($"uncovered: {gap.Start:HH:mm}-{gap.End:HH:mm}");
    }

    public static void LowestPrice()
    {
        decimal[] prices = new decimal[60];
        for (int d = 0; d < prices.Length; d++) prices[d] = 20m;
        for (int d = 10; d < 14; d++) prices[d] = 15m; // a short sale 46-49 days ago
        for (int d = 40; d < 55; d++) prices[d] = 24m; // raised before the "discount"
        prices[55] = 18m;
        decimal[] min30 = SlidingWindow.Minimums(prices, 30); // window ends at the given day, inclusive
        foreach (int d in new[] { 13, 39, 43, 54 })
            Console.WriteLine($"day {d,2}: price {prices[d],5}, lowest of last 30 days {min30[d],5}");
        decimal prior = min30[54]; // the 30 days before the discount day
        Console.WriteLine($"discount on day 55: new price {prices[55]}, yesterday {prices[54]} ({1 - prices[55] / prices[54]:P0} off), " +
                          $"prior price {prior} ({1 - prices[55] / prior:P0} off)");
    }

    public static void TimeZones()
    {
        TimeZoneInfo istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        DateTimeOffset[] deliveredUtc =
        [
            new(2026, 11, 15, 20, 45, 0, TimeSpan.Zero),
            new(2026, 11, 15, 22, 30, 0, TimeSpan.Zero),
            new(2026, 11, 16, 7, 15, 0, TimeSpan.Zero),
        ];
        foreach (IGrouping<DateOnly, DateTimeOffset> g in deliveredUtc.GroupBy(d => DateOnly.FromDateTime(d.UtcDateTime)))
            Console.WriteLine($"by UTC date   {g.Key:yyyy-MM-dd}: {g.Count()}");
        foreach (IGrouping<DateOnly, DateTimeOffset> g in deliveredUtc.GroupBy(d => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(d, istanbul).DateTime)))
            Console.WriteLine($"by local date {g.Key:yyyy-MM-dd}: {g.Count()}");
    }
}
