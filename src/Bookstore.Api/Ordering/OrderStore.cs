using System.Collections.Concurrent;

namespace Bookstore.Ordering;

public enum OrderStatus
{
    Placed,
    PaymentPending,
    Paid,
    PaymentDeclined
}

public sealed class Order
{
    public required int Id { get; init; }
    public required string CustomerName { get; init; }
    public required string ShipToPostalCode { get; init; }
    public required decimal ItemsTotal { get; init; }
    public required int WeightGrams { get; init; }
    public string Currency { get; init; } = "TRY";
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public decimal? ShippingPrice { get; set; }
    public string? ProviderPaymentId { get; set; }
}

// In-memory orders: this series is about outbound calls, not persistence.
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, Order> _orders = new()
    {
        [1001] = new Order { Id = 1001, CustomerName = "Ada", ShipToPostalCode = "06100", ItemsTotal = 459.00m, WeightGrams = 850 },
        [1002] = new Order { Id = 1002, CustomerName = "Alan", ShipToPostalCode = "35210", ItemsTotal = 312.50m, WeightGrams = 600 }
    };

    public Order? Find(int id) => _orders.GetValueOrDefault(id);
}
