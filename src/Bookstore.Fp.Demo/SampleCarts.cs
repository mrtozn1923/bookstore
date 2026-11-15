using Bookstore.Ordering.Legacy;

namespace Bookstore.Fp.Demo;

public static class SampleCarts
{
    public static List<LegacyCartLine> Legacy() =>
    [
        new() { Isbn = "9780441013593", Title = "Dune", UnitPrice = 189.50m, Quantity = 1 },
        new() { Isbn = "9780141439587", Title = "Emma", UnitPrice = 59.90m, Quantity = 1, IsEbook = true },
        new() { Isbn = "9780261103344", Title = "The Hobbit", UnitPrice = 149.90m, Quantity = 0 }
    ];
}
