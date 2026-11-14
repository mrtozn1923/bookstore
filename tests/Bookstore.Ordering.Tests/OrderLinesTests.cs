using static Bookstore.Ordering.Tests.Books;

namespace Bookstore.Ordering.Tests;

public sealed class OrderLinesTests
{
    private readonly Order order = DuneAndHobbit();

    [Fact]
    public void New_cart_is_empty() => Assert.Empty(new Cart().Lines);

    [Fact]
    public void Lines_keep_the_cart_order()
    {
        Assert.Collection(order.Lines,
            first => Assert.Equal(new OrderLine(Dune, 2, 12.50m), first),
            second => Assert.Equal(Hobbit, second.Isbn));
    }

    [Fact]
    public void Every_line_has_a_positive_quantity_and_price()
    {
        Assert.All(order.Lines, line =>
        {
            Assert.True(line.Quantity > 0);
            Assert.True(line.UnitPrice > 0m);
        });
    }

    [Fact]
    public void Lines_are_searched_by_condition()
    {
        Assert.Contains(order.Lines, l => l.Isbn == Dune && l.Quantity == 2);
        Assert.DoesNotContain(order.Lines, l => l.Isbn == Emma);
        Assert.Distinct(order.Lines.Select(l => l.Isbn));
    }

    [Fact]
    public void Ordered_isbns_come_from_the_catalog()
    {
        HashSet<string> catalog = [Dune, Hobbit, Emma];

        Assert.Subset(catalog, order.Lines.Select(l => l.Isbn).ToHashSet());
    }

    [Fact]
    public void Lines_match_regardless_of_order()
    {
        Assert.Equivalent(
            new[]
            {
                new { Isbn = Hobbit, Quantity = 1, UnitPrice = 9.90m },
                new { Isbn = Dune, Quantity = 2, UnitPrice = 12.50m },
            },
            order.Lines,
            strict: true);
    }

    [Fact]
    public void Order_from_the_same_cart_differs_only_by_id()
    {
        Cart cart = new Cart()
            .Add(Dune, "Dune", 12.50m, quantity: 2)
            .Add(Hobbit, "The Hobbit", 9.90m);

        Order created = Order.FromCart(Guid.NewGuid(), "BS-2026-000123", "ada@example.com", cart);

        Assert.EquivalentWithExclusions(order, created, o => o.Id);
    }
}
