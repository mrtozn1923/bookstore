using Bookstore.Catalog;
using Bookstore.Ordering;

namespace Bookstore.Notifications.Tests;

public sealed class FakeSmsGateway : ISmsGateway
{
    private readonly List<string> _sent = [];

    public IReadOnlyList<string> Sent => _sent;

    public void Send(string phone, string text) => _sent.Add($"{phone}: {text}");
}

public static class TestData
{
    public static readonly Dictionary<int, string> Phones = new() { [1] = "555-0101", [2] = "555-0102" };
    public static readonly HashSet<int> SmsOptOut = [2];

    public static Order OrderOf(int customerId, int books)
    {
        Order order = new() { Customer = new Customer { Id = customerId, Name = $"Customer {customerId}" } };
        if (books >= 1)
            order.AddItem(new OrderItem(Isbn.Parse("9780441013593"), "Dune", 120m, 1));
        if (books >= 2)
            order.AddItem(new OrderItem(Isbn.Parse("9780141439587"), "Emma", 59.90m, 1));
        return order;
    }
}
