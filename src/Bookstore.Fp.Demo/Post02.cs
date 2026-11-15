using System.Collections.Frozen;
using System.Collections.Immutable;
using Bookstore.Ordering.Carts;
using Bookstore.Ordering.Legacy;

namespace Bookstore.Fp.Demo;

public static class Post02
{
    public static void Aliasing()
    {
        LegacyCart cart = new() { Lines = SampleCarts.Legacy() };
        LegacyOrder order = LegacyOrder.PlaceFrom(cart);

        // The customer keeps shopping after placing the order
        cart.Lines.Add(new LegacyCartLine { Isbn = "9780141439549", Title = "Middlemarch", UnitPrice = 129.90m, Quantity = 1 });
        cart.Lines[0].Quantity = 3;

        Console.WriteLine($"order lines: {order.Lines.Count}, Dune quantity in order: {order.Lines[0].Quantity}");
    }

    public static void CacheKey()
    {
        Dictionary<PriceQuery, decimal> priceCache = [];
        PriceQuery query = new() { Isbn = "9780441013593", Currency = "TRY" };
        priceCache[query] = 189.50m;

        query.Currency = "EUR";

        Console.WriteLine($"same object found: {priceCache.ContainsKey(query)}");
        Console.WriteLine($"original key found: {priceCache.ContainsKey(new PriceQuery { Isbn = "9780441013593", Currency = "TRY" })}");
        Console.WriteLine($"entries: {priceCache.Count}");
    }

    public static void Records()
    {
        Cart cart = Cart.Empty
            .Add(new CartLine("9780441013593", "Dune", 189.50m, 1, IsEbook: false))
            .Add(new CartLine("9780141439587", "Emma", 59.90m, 1, IsEbook: true))
            .Add(new CartLine("9780261103344", "The Hobbit", 149.90m, 0, IsEbook: false));

        Cart cleaned = cart.WithoutEmptyLines();
        Cart withCoupon = cleaned with { CouponCode = "AUTUMN10" };
        Cart moreDune = withCoupon.ChangeQuantity("9780441013593", 3);

        Console.WriteLine($"cart: {cart.Lines.Count} lines, cleaned: {cleaned.Lines.Count}, coupon on cleaned: {cleaned.CouponCode ?? "none"}");
        Console.WriteLine($"Dune in withCoupon: {withCoupon.Lines[0].Quantity}, in moreDune: {moreDune.Lines[0].Quantity}");
        Console.WriteLine($"shared Emma line: {ReferenceEquals(withCoupon.Lines[1], moreDune.Lines[1])}");

        Cart rebuilt = Cart.Empty
            .Add(new CartLine("9780441013593", "Dune", 189.50m, 1, IsEbook: false))
            .Add(new CartLine("9780141439587", "Emma", 59.90m, 1, IsEbook: true));
        Console.WriteLine($"lines equal: {cleaned.Lines.SequenceEqual(rebuilt.Lines)}, carts equal: {cleaned == rebuilt}");
    }

    public static void Shallow()
    {
        ShallowCart first = new([new CartLine("9780441013593", "Dune", 189.50m, 1, IsEbook: false)]);
        ShallowCart second = first with { CouponCode = "AUTUMN10" };

        second.Lines.Add(new CartLine("9780141439587", "Emma", 59.90m, 1, IsEbook: true));

        Console.WriteLine($"first: {first.Lines.Count} lines, second: {second.Lines.Count} lines, same list: {ReferenceEquals(first.Lines, second.Lines)}");

        IReadOnlyList<CartLine> readOnly = first.Lines;
        ((List<CartLine>)readOnly).Clear();
        Console.WriteLine($"after cast and Clear: {first.Lines.Count} lines");
    }

    public static void CopyCost()
    {
        CartLine line = new("9780441013593", "Dune", 189.50m, 1, IsEbook: false);
        ImmutableArray<CartLine> array = Enumerable.Repeat(line, 10_000).ToImmutableArray();
        ImmutableList<CartLine> list = Enumerable.Repeat(line, 10_000).ToImmutableList();

        long before = GC.GetAllocatedBytesForCurrentThread();
        ImmutableArray<CartLine> array2 = array.Add(line);
        long arrayBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        ImmutableList<CartLine> list2 = list.Add(line);
        long listBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Console.WriteLine($"ImmutableArray.Add: {arrayBytes} bytes, ImmutableList.Add: {listBytes} bytes ({array2.Length}, {list2.Count})");

        FrozenDictionary<string, decimal> couponRates = new Dictionary<string, decimal>
        {
            ["AUTUMN10"] = 0.10m,
            ["WELCOME5"] = 0.05m
        }.ToFrozenDictionary();
        Console.WriteLine($"AUTUMN10: {couponRates["AUTUMN10"]}, coupons: {couponRates.Count}");
    }
}
