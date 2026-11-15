using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class CheckoutServiceTests
{
    private static readonly Dictionary<string, List<CartLine>> Carts = new()
    {
        ["empty"] = [],
        ["one paperback"] = [new("9780441013593", "Dune", 12.45m, 1, false)],
        ["two hardcovers"] = [new("9780261103344", "The Hobbit", 149.90m, 2, false)],
        ["ebook only"] = [new("9780141439587", "Emma", 59.90m, 1, true)],
        ["mixed"] = [new("9780261103344", "The Hobbit", 189.50m, 1, false), new("9780141439549", "Middlemarch", 129.90m, 1, true)]
    };

    [Fact]
    public Task Calculate_for_all_carts() =>
        Combination()
            .Verify(
                (string cart, bool isExpress) => new CheckoutService().CalculateTotals(Carts[cart], isExpress),
                Carts.Keys,
                [false, true]);

    [Fact]
    public void Express_adds_surcharge_even_above_free_shipping_threshold()
    {
        CheckoutTotals totals = new CheckoutService().CalculateTotals(Carts["mixed"], isExpress: true);

        Assert.Equal(35m, totals.Shipping);
    }
}
