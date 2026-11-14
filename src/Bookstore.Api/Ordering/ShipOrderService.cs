using Bookstore.Webhooks;

namespace Bookstore.Ordering;

public sealed record ShipOrderResult(int OrderId, OrderStatus Status, string ShipmentId, int WebhooksQueued);

public sealed class ShipOrderService(OrderStore orders, WebhookPublisher webhooks, ILogger<ShipOrderService> logger)
{
    public ShipOrderResult? Ship(int orderId, string trackingNumber)
    {
        var order = orders.Find(orderId);
        if (order is null)
        {
            return null;
        }

        if (order.Status != OrderStatus.Paid)
        {
            throw new OrderStateException($"Order {orderId} is {order.Status}, only paid orders can be shipped.");
        }

        order.Status = OrderStatus.Shipped;
        var shipmentId = $"SHP-{orderId}";
        var queued = 0;
        if (order.InstitutionId is { } institutionId)
        {
            // Only an outbox write here: the request does not wait for the customer's endpoint.
            queued = webhooks.Publish(institutionId, WebhookEventTypes.OrderShipped,
                new OrderShippedData(order.Id, shipmentId, trackingNumber));
        }

        logger.LogInformation("Order {OrderId} shipped, {Count} webhook(s) queued", orderId, queued);
        return new ShipOrderResult(order.Id, order.Status, shipmentId, queued);
    }
}

public sealed class OrderStateException(string message) : Exception(message);
