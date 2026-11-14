using static Bookstore.Ordering.Tests.Books;

namespace Bookstore.Ordering.Tests;

public sealed class CartTotalTests
{
    [Fact]
    public void Subtotal_is_the_sum_of_line_totals()
    {
        Cart cart = new Cart()
            .Add(Dune, "Dune", 12.50m, quantity: 2)
            .Add(Hobbit, "The Hobbit", 9.90m);

        Assert.Equal(34.90m, cart.Subtotal);
    }

    [Fact]
    public void Institutional_discount_is_rounded_to_cents()
    {
        // 3 x 12.45 = 37.35; with 10% off 33.615 -> 33.62
        Cart cart = new Cart().Add(Dune, "Dune", 12.45m, quantity: 3);

        Assert.Equal(33.62m, cart.Total(discountRate: 0.10m));
    }

    [Fact]
    public void Discounted_total_never_exceeds_the_subtotal()
    {
        Cart cart = new Cart().Add(Emma, "Emma", 8.75m, quantity: 4);

        Assert.InRange(cart.Total(discountRate: 0.10m), 0m, cart.Subtotal);
    }

    [Fact]
    public void Parcel_weight_is_compared_with_a_tolerance()
    {
        Cart cart = new Cart().Add(Hobbit, "The Hobbit", 9.90m, quantity: 3, weightKg: 0.1);

        Assert.Equal(0.3, cart.WeightKg, tolerance: 0.000001);
    }

    [Fact]
    public void Add_returns_the_same_cart_for_chaining()
    {
        Cart cart = new();

        Assert.Same(cart, cart.Add(Dune, "Dune", 12.50m));
    }

    [Fact]
    public void Adding_the_same_isbn_twice_merges_the_line()
    {
        Cart cart = new Cart()
            .Add(Dune, "Dune", 12.50m)
            .Add(Dune, "Dune", 12.50m);

        CartLine line = Assert.Single(cart.Lines);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public void Unknown_isbn_is_not_found()
    {
        Cart cart = new Cart().Add(Dune, "Dune", 12.50m);

        Assert.Null(cart.Find(Hobbit));
        Assert.NotNull(cart.Find(Dune));
    }
}
