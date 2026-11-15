namespace Bookstore.Api.Ordering;

public sealed record Order(string Id, Guid CustomerId, string Status, DateTime PlacedAtUtc, IReadOnlyList<string> Isbns);

public interface IOrderRepository
{
    Order? Find(string orderId);
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    public static readonly Guid Ada = Guid.Parse("6f1c2a54-0d7e-4b8a-9a3f-1d2e3c4b5a60");
    public static readonly Guid Alan = Guid.Parse("0b9e8d7c-6a5f-4e3d-8c2b-1a0f9e8d7c6b");

    private static readonly Order[] Orders =
    [
        new("1042", Ada, "Shipped", new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc), ["9780441172719"]),
        new("1043", Alan, "Preparing", new DateTime(2026, 11, 14, 16, 5, 0, DateTimeKind.Utc), ["9780141439587", "9780547928227"]),
    ];

    public Order? Find(string orderId) => Orders.FirstOrDefault(o => o.Id == orderId);
}

// The signed-in customer; authentication itself is covered in the ASP.NET Core Web API series.
public sealed record CurrentCustomer(Guid Id);
