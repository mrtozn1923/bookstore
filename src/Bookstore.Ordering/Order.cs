using Bookstore.Catalog;

namespace Bookstore.Ordering;

public static class OrderSettings
{
    private static int s_maxQuantityPerLine = 10;

    public static int MaxQuantityPerLine
    {
        get => s_maxQuantityPerLine;
        set => s_maxQuantityPerLine = value >= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The limit must be at least 1.");
    }
}

public sealed record OrderItem(Isbn Isbn, string Title, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record Address(string Street, string City, string PostalCode);

public class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public Address Address { get; set; } = new("", "", "");
    public int LoyaltyPoints { get; set; }
}

public class Order
{
    private readonly List<OrderItem> _items = [];

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.LineTotal);
    public Customer Customer { get; init; } = new();
    public Address ShippingAddress { get; init; } = new("", "", "");

    public void AddItem(OrderItem item)
    {
        if (item.Quantity > OrderSettings.MaxQuantityPerLine)
            throw new InvalidOperationException($"At most {OrderSettings.MaxQuantityPerLine} copies per line.");
        _items.Add(item);
    }

    public bool Contains(Isbn isbn) => Items.Any(i => i.Isbn == isbn);

    public decimal DiscountedTotal(decimal rate)
    {
        decimal discount = Total * rate;
        decimal net = Total - discount;
        return Math.Round(net, 2, MidpointRounding.AwayFromZero);
    }

    public string Summary()
    {
        List<string> lines = [];
        foreach (OrderItem item in Items)
        {
            lines.Add($"{item.Quantity} x {item.Title} = {item.LineTotal}");
        }
        return string.Join(Environment.NewLine, lines);
    }
}
