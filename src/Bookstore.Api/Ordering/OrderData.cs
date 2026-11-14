using Bookstore.Api.Identity;

namespace Bookstore.Api.Ordering;

public sealed class OrderData
{
    private readonly List<Order> _orders =
    [
        Individual("01a06655-e3a0-7a3b-8199-c6b41c80317f", "ada", OrderStatus.Delivered, "2026-09-03T08:15:00Z",
            [new("9780441172719", "Dune", 1, 489.00m)]),
        Individual("01a15eb0-af80-7bdd-8fac-4ee446685257", "ada", OrderStatus.Cancelled, "2026-10-21T13:40:00Z",
            [new("9780141439587", "Emma", 1, 249.90m)]),
        Individual("01a1cfa1-0a00-7b74-ace2-8223a65ed389", "ada", OrderStatus.Confirmed, "2026-11-12T12:00:00Z",
            [new("9780261102217", "The Hobbit", 1, 349.00m), new("9780451524935", "Nineteen Eighty-Four", 1, 219.00m)]),
        Institutional("01a0a3f9-00c0-7392-af22-582a23b8c1e9", "lakeside-school", "main-campus", OrderStatus.Delivered,
            "2026-09-15T07:30:00Z", [new("9780141439587", "Emma", 30, 224.91m)]),
        Institutional("01a12f4d-f2c0-73b8-a687-7991815ef6d1", "lakeside-school", "main-campus", OrderStatus.Shipped,
            "2026-10-12T08:50:00Z", [new("9780061120084", "To Kill a Mockingbird", 25, 260.10m)]),
        Institutional("01a1c112-d7c0-706c-8cb9-c18a8fadc1a6", "lakeside-school", "main-campus", OrderStatus.Pending,
            "2026-11-09T16:10:00Z", [new("9780451524935", "Nineteen Eighty-Four", 40, 197.10m)]),
        Institutional("01a1d3b8-5e40-7f2a-9c61-3d8e0b47a915", "lakeside-school", "main-campus", OrderStatus.PartiallyShipped,
            "2026-11-13T09:30:00Z", [new("9780553293357", "Foundation", 30, 269.10m), new("9780441172719", "Dune", 30, 440.10m)]),
        Institutional("01a0fbdc-1660-71a3-af67-19acad3c2d6d", "lakeside-school", "east-campus", OrderStatus.Delivered,
            "2026-10-02T09:05:00Z", [new("9780141441146", "Jane Eyre", 20, 233.10m)]),
        Institutional("01a1a612-9c80-7e46-8590-67e08b9d2434", "lakeside-school", "east-campus", OrderStatus.Confirmed,
            "2026-11-04T10:20:00Z", [new("9780553293357", "Foundation", 24, 269.10m)]),
        Institutional("01a0c8c5-c780-7972-8208-ba3c6c031199", "city-library", "central", OrderStatus.Delivered,
            "2026-09-22T11:00:00Z", [new("9780142437247", "Moby-Dick", 20, 305.10m)]),
        Institutional("01a18d45-6de0-707a-8dfe-2a2217fc695a", "city-library", "central", OrderStatus.Confirmed,
            "2026-10-30T14:45:00Z", [new("9781400079988", "War and Peace", 20, 494.10m)]),
    ];

    private readonly List<OrderReturn> _returns = [];

    public IEnumerable<Order> All => _orders;

    public Order? Find(Guid id) => _orders.Find(o => o.Id == id);

    // One visibility rule for every URL that reaches an order.
    public static bool CanSee(DemoUser user, Order order) => user.Role switch
    {
        "Customer" => order.CustomerId == user.Name,
        "InstitutionApprover" => order.InstitutionId == user.InstitutionId,
        "InstitutionBuyer" => order.InstitutionId == user.InstitutionId && order.UnitId == user.UnitId,
        "StoreStaff" => true,
        _ => false,
    };

    public Order Add(string customerId, IReadOnlyList<OrderItem> items)
    {
        Order order = new()
        {
            Id = Guid.CreateVersion7(), CustomerId = customerId, Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow, Items = items,
        };
        lock (_orders)
        {
            _orders.Add(order);
        }

        return order;
    }

    public OrderReturn AddReturn(Order order, string reason)
    {
        OrderReturn result = new(Guid.CreateVersion7(), order.Id, ReturnStatus.Requested, reason, DateTime.UtcNow);
        _returns.Add(result);
        return result;
    }

    public OrderReturn? FindReturn(Guid orderId, Guid returnId) =>
        _returns.Find(r => r.OrderId == orderId && r.Id == returnId);

    private static Order Individual(string id, string customerId, OrderStatus status, string createdAt, OrderItem[] items) =>
        new() { Id = Guid.Parse(id), CustomerId = customerId, Status = status, CreatedAt = DateTimeOffset.Parse(createdAt).UtcDateTime, Items = items };

    private static Order Institutional(string id, string institutionId, string unitId, OrderStatus status, string createdAt,
        OrderItem[] items) =>
        new()
        {
            Id = Guid.Parse(id), InstitutionId = institutionId, UnitId = unitId, Status = status,
            CreatedAt = DateTimeOffset.Parse(createdAt).UtcDateTime, Items = items,
        };
}
