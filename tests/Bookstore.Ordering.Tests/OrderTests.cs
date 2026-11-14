using Bookstore.Ordering.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Ordering.Tests;

public sealed class OrderTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));
    private static readonly Buyer Ada = new("ada-subject", "Ada Lovelace", "ada@example.com");

    private static OrderLine Dune(int quantity = 1) => new("9780441172719", "Dune", new Money(459.00m, "TRY"), quantity);
    private static OrderLine Emma(int quantity = 1) => new("9780141439587", "Emma", new Money(249.90m, "TRY"), quantity);

    [Fact]
    public void A_new_order_is_pending_and_raises_OrderPlaced()
    {
        Order order = Order.Place(Ada, [Dune(), Emma(2)], Clock);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(new Money(958.80m, "TRY"), order.Total);
        OrderPlaced placed = Assert.IsType<OrderPlaced>(Assert.Single(order.DomainEvents));
        Assert.Equal(3, placed.ItemCount);
    }

    [Fact]
    public void The_same_book_twice_becomes_one_line()
    {
        Order order = Order.Place(Ada, [Dune(2), Dune(3)], Clock);

        OrderItem item = Assert.Single(order.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(new Money(2295.00m, "TRY"), item.LineTotal);
    }

    [Fact]
    public void The_unit_price_is_frozen_on_the_line()
    {
        OrderLine line = Dune();
        Order order = Order.Place(Ada, [line], Clock);

        Assert.Equal(459.00m, Assert.Single(order.Items).UnitPrice.Amount);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(6, 5)]
    public void More_than_ten_copies_of_a_book_are_rejected(params int[] quantities)
    {
        DomainException error = Assert.Throws<DomainException>(
            () => Order.Place(Ada, [.. quantities.Select(q => Dune(q))], Clock));

        Assert.Equal("order.too-many-copies", error.Code);
    }

    [Fact]
    public void An_order_needs_at_least_one_item() =>
        Assert.Equal("order.empty", Assert.Throws<DomainException>(() => Order.Place(Ada, [], Clock)).Code);

    [Fact]
    public void Currencies_cannot_be_mixed()
    {
        OrderLine euroEmma = Emma() with { UnitPrice = new Money(12.50m, "EUR") };

        Assert.Equal("order.mixed-currency",
            Assert.Throws<DomainException>(() => Order.Place(Ada, [Dune(), euroEmma], Clock)).Code);
    }

    [Fact]
    public void A_cancelled_order_cannot_be_cancelled_again()
    {
        Order order = Order.Place(Ada, [Dune()], Clock);
        order.Cancel("customer request", Clock);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("order.not-cancellable",
            Assert.Throws<DomainException>(() => order.Cancel("again", Clock)).Code);
    }
}
