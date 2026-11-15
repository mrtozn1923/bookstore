using Bookstore.Ordering.Carts;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Fp.Demo;

public static class Post04
{
    private static readonly CartLine[] Lines =
    [
        new("9780441013593", "Dune", 189.50m, 1, IsEbook: false),
        new("9780141439587", "Emma", 59.90m, 1, IsEbook: true),
        new("9780261103344", "The Hobbit", 149.90m, 0, IsEbook: false)
    ];

    public static void Fold()
    {
        decimal subtotal = Lines.Aggregate(0m, (sum, line) => sum + line.LineTotal);
        decimal maxPrice = Lines.Aggregate(decimal.MinValue, (max, line) => Math.Max(max, line.UnitPrice));
        bool anyEbook = Lines.Aggregate(false, (found, line) => found || line.IsEbook);
        List<string> titles = Lines.Aggregate(new List<string>(), (list, line) => line.Quantity > 0 ? [.. list, line.Title] : list);

        Console.WriteLine($"subtotal: {subtotal}, max: {maxPrice}, ebook: {anyEbook}, titles: {string.Join(", ", titles)}");
    }

    public static void Composition()
    {
        Func<decimal, decimal> couponThenMember = PriceRules.PercentOff(0.10m).Then(PriceRules.AmountOff(15m)).Then(PriceRules.RoundToKurus);
        Func<decimal, decimal> memberThenCoupon = PriceRules.AmountOff(15m).Then(PriceRules.PercentOff(0.10m)).Then(PriceRules.RoundToKurus);
        Console.WriteLine($"coupon then member: {couponThenMember(249.40m)}");
        Console.WriteLine($"member then coupon: {memberThenCoupon(249.40m)}");

        Func<decimal, decimal> sameAsFirst = PriceRules.RoundToKurus.Compose(PriceRules.AmountOff(15m)).Compose(PriceRules.PercentOff(0.10m));
        Console.WriteLine($"Compose, mathematical order: {sameAsFirst(249.40m)}");

        List<Func<decimal, decimal>> rules = [PriceRules.PercentOff(0.10m), PriceRules.AmountOff(15m), PriceRules.RoundToKurus];
        Console.WriteLine($"Combine(list): {PriceRules.Combine(rules)(249.40m)}, Combine(empty): {PriceRules.Combine([])(249.40m)}");

        decimal piped = 249.40m.Pipe(PriceRules.PercentOff(0.10m)).Pipe(PriceRules.AmountOff(15m)).Pipe(PriceRules.RoundToKurus);
        Console.WriteLine($"Pipe: {piped}");
    }

    public static void Currying()
    {
        Func<decimal, decimal, decimal> applyRate = (rate, amount) => Math.Round(amount * (1 - rate), 2, MidpointRounding.AwayFromZero);

        Func<decimal, Func<decimal, decimal>> curried = applyRate.Curry();
        Func<decimal, decimal> autumn = curried(0.10m);
        Func<decimal, decimal> welcome = applyRate.Partial(0.05m);

        decimal[] prices = [189.50m, 59.90m, 149.90m];
        Console.WriteLine($"autumn: {string.Join(", ", prices.Select(autumn))}");
        Console.WriteLine($"welcome: {string.Join(", ", prices.Select(welcome))}");
    }

    public static void Allocations()
    {
        decimal[] prices = Enumerable.Range(1, 1_000).Select(i => 100m + i).ToArray();
        decimal total = 0;

        long before = GC.GetAllocatedBytesForCurrentThread();
        foreach (decimal price in prices)
            total += PriceRules.PercentOff(0.10m)(price);
        long perItem = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        Func<decimal, decimal> autumn = PriceRules.PercentOff(0.10m);
        foreach (decimal price in prices)
            total += autumn(price);
        long once = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        foreach (decimal price in prices)
            total += price * 0.90m;
        long plain = GC.GetAllocatedBytesForCurrentThread() - before;

        Console.WriteLine($"rule created per item: {perItem} bytes, created once: {once} bytes, no delegate: {plain} bytes ({total})");
    }

    public static void MultipleEnumeration()
    {
        int priceLookups = 0;
        IEnumerable<decimal> discounted = Lines
            .Where(line => line.Quantity > 0)
            .Select(line =>
            {
                priceLookups++;
                return PriceRules.PercentOff(0.10m).Then(PriceRules.RoundToKurus)(line.LineTotal);
            });

        decimal sum = discounted.Sum();
        decimal max = discounted.Max();
        Console.WriteLine($"sum: {sum}, max: {max}, price lookups: {priceLookups}");
    }
}
