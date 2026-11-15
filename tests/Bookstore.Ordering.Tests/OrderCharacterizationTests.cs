using Bookstore.Catalog;
using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class OrderCharacterizationTests
{
    private static Customer Ada() => new()
    {
        Id = 7,
        Name = "Ada",
        Address = new Address("1 Library Lane", "Ankara", "06100")
    };

    [Fact]
    public void Total_is_kept_when_items_are_added_through_AddItem()
    {
        Order order = new();
        order.AddItem(new OrderItem(Isbn.Parse("978-0-441-01359-3"), "Dune", 12.45m, 1));
        order.AddItem(new OrderItem(Isbn.Parse("9780261103344"), "The Hobbit", 149.90m, 2));

        Assert.Equal(312.25m, order.Total);
        Assert.Equal(281.03m, order.DiscountedTotal(0.10m));
        Assert.Equal($"1 x Dune = 12.45{Environment.NewLine}2 x The Hobbit = 299.80", order.Summary());
    }

    [Fact]
    public void AddItem_rejects_more_copies_than_the_limit()
    {
        Order order = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(new OrderItem(Isbn.Parse("9780261103344"), "The Hobbit", 149.90m, 11)));
        Assert.Equal("At most 10 copies per line.", ex.Message);
    }

    [Fact]
    public void Items_cannot_be_changed_by_casting_to_List()
    {
        Order order = new();
        order.AddItem(new OrderItem(Isbn.Parse("978-0-441-01359-3"), "Dune", 12.45m, 1));

        Assert.Throws<InvalidCastException>(() => ((List<OrderItem>)order.Items).Add(
            new OrderItem(Isbn.Parse("9780261103344"), "The Hobbit", 149.90m, 2)));
        Assert.Equal(12.45m, order.Total);
    }

    [Fact]
    public void Contains_finds_the_same_isbn_written_with_or_without_hyphens()
    {
        Order order = new();
        order.AddItem(new OrderItem(Isbn.Parse("978-0-441-01359-3"), "Dune", 12.45m, 1));

        Assert.True(order.Contains(Isbn.Parse("9780441013593")));
    }

    [Fact]
    public void Shipping_address_keeps_its_value_when_the_customer_moves()
    {
        Customer ada = Ada();
        Order order = new() { Customer = ada, ShippingAddress = ada.Address };

        ada.Address = ada.Address with { City = "Izmir" };

        Assert.Equal("Ankara", order.ShippingAddress.City);
        Assert.Equal("Izmir", ada.Address.City);
    }
}
