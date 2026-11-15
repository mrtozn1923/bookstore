using System.Collections.Concurrent;

namespace Bookstore.Ordering;

public enum OrderStatus
{
    Placed,
    PaymentPending,
    Paid,
    PaymentDeclined,
    Shipped
}

public sealed class Order
{
    public required int Id { get; init; }
    public required string CustomerName { get; init; }
    public required string CustomerEmail { get; init; }
    public required string ShipToPostalCode { get; init; }
    public required decimal ItemsTotal { get; init; }
    public required int WeightGrams { get; init; }
    public string Currency { get; init; } = "TRY";
    // Set for institutional (B2B) orders; their webhooks go to the institution's endpoints.
    public string? InstitutionId { get; init; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public decimal? ShippingPrice { get; set; }
    public string? ProviderPaymentId { get; set; }
}

// In-memory orders: this series is about outbound calls, not persistence.
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new()
    {
        [1001] = new Order { Id = 1001, CustomerName = "Ada", CustomerEmail = "ada@example.com", ShipToPostalCode = "06100", ItemsTotal = 459.00m, WeightGrams = 850 },
        [1002] = new Order { Id = 1002, CustomerName = "Alan", CustomerEmail = "alan@example.com", ShipToPostalCode = "35210", ItemsTotal = 312.50m, WeightGrams = 600 },
        [1003] = new Order { Id = 1003, CustomerName = "Linus", CustomerEmail = "linus@school-0042.example.org", ShipToPostalCode = "06420", ItemsTotal = 4131.00m, WeightGrams = 9800, InstitutionId = "school-0042" },
        [1004] = new Order { Id = 1004, CustomerName = "Grace", CustomerEmail = "grace@library-0007.example.org", ShipToPostalCode = "34710", ItemsTotal = 2250.00m, WeightGrams = 5400, InstitutionId = "library-0007" }
    };

    public Order? Find(int id) => _orders.GetValueOrDefault(id);

    public void Add(Order order) => _orders[order.Id] = order;
}
