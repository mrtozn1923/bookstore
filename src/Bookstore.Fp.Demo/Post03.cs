using System.Globalization;
using Bookstore.Ordering.Carts;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Fp.Demo;

public static class Post03
{
    public static readonly Campaign Autumn = new(
        "AUTUMN10", 0.10m,
        new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(3)),
        new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(3)));

    public static void Culture()
    {
        foreach (string name in new[] { "en-US", "tr-TR" })
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            Console.WriteLine($"{name}: ParseLegacy(\"189.50\") = {PriceText.ParseLegacy("189.50").ToString(CultureInfo.InvariantCulture)}, Parse(\"189.50\") = {PriceText.Parse("189.50").ToString(CultureInfo.InvariantCulture)}");
        }
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public static void CoreAndShell()
    {
        Cart cart = Cart.Empty
            .Add(new CartLine("9780441013593", "Dune", 189.50m, 1, IsEbook: false))
            .Add(new CartLine("9780141439587", "Emma", 59.90m, 1, IsEbook: true))
            .Add(new CartLine("9780261103344", "The Hobbit", 149.90m, 0, IsEbook: false))
            with { CouponCode = "AUTUMN10" };

        DateTimeOffset lastMinute = new(2026, 11, 30, 23, 59, 0, TimeSpan.FromHours(3));
        DateTimeOffset nextDay = new(2026, 12, 1, 0, 1, 0, TimeSpan.FromHours(3));
        Console.WriteLine($"core, last minute: {CheckoutCalculator.Calculate(cart, isMember: true, Autumn, lastMinute)}");
        Console.WriteLine($"core, next day:    {CheckoutCalculator.Calculate(cart, isMember: true, Autumn, nextDay).Total}");

        CheckoutService service = new(TimeProvider.System, Autumn, Console.Out);
        CheckoutTotals totals = service.Checkout(cart, isMember: true);
        Console.WriteLine($"shell total: {totals.Total}, cart lines: {cart.Lines.Count}");
    }

    public static void Memoization()
    {
        int calls = 0;
        Func<string, bool> isValidIsbn = isbn =>
        {
            calls++;
            int sum = isbn.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
            return isbn.Length == 13 && sum % 10 == 0;
        };
        Func<string, bool> cachedIsbn = isValidIsbn.Memoize();
        bool[] results = [cachedIsbn("9780441013593"), cachedIsbn("9780441013593"), cachedIsbn("9780441013594")];
        Console.WriteLine($"pure: {string.Join(", ", results)} with {calls} calls");

        DateTimeOffset clock = new(2026, 11, 30, 23, 59, 0, TimeSpan.FromHours(3));
        Func<string, bool> isCouponActive = code => code == Autumn.Code && Autumn.IsActiveAt(clock);
        Func<string, bool> cachedCoupon = isCouponActive.Memoize();
        Console.WriteLine($"23:59 -> cached: {cachedCoupon("AUTUMN10")}, direct: {isCouponActive("AUTUMN10")}");
        clock = clock.AddMinutes(2);
        Console.WriteLine($"00:01 -> cached: {cachedCoupon("AUTUMN10")}, direct: {isCouponActive("AUTUMN10")}");
    }
}
