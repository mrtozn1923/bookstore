namespace Bookstore.Ordering;

public sealed class OrderService(ILogger<OrderService> logger, IOrderRepository orders)
{
    public async Task<Order> CreateAsync(Cart cart, CancellationToken ct)
    {
        Order order = Order.From(cart);
        await orders.AddAsync(order, ct);
        logger.LogInformation($"Order {order.Number} created");
        return order;
    }

    public async Task ShipAsync(string orderNumber, CancellationToken ct)
    {
        await orders.MarkShippedAsync(orderNumber, ct);
        logger.LogInformation($"Order {orderNumber} shipped");
    }

    public async Task CancelAsync(string orderNumber, string reason, CancellationToken ct)
    {
        await orders.CancelAsync(orderNumber, ct);
        logger.LogWarning($"Order {orderNumber} cancelled: {reason}");
    }
}
