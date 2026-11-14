using Bookstore.Contracts.Ordering;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Live;
using Microsoft.AspNetCore.SignalR;

namespace Bookstore.Notifications.Orders;

// The checkout saga's results reach the customer twice: a live push to every open tab, and an e-mail.
public static class OrderEventsHandler
{
    public static async Task Handle(OrderPlaced message, IHubContext<OrderStatusHub, IOrderStatusClient> hub, ILogger logger)
    {
        logger.LogInformation("Order {OrderId} placed by {CustomerId}: {Total} {Currency}, {Lines} line(s)",
            message.OrderId, message.CustomerId, message.Total, message.Currency, message.Lines.Count);
        await hub.Clients.User(message.CustomerId).OrderStatusChanged(new(message.OrderId, "Pending", null));
    }

    public static async Task Handle(
        OrderConfirmed message, IHubContext<OrderStatusHub, IOrderStatusClient> hub, IOrderEmails emails, CancellationToken ct)
    {
        await hub.Clients.User(message.CustomerId).OrderStatusChanged(new(message.OrderId, "Confirmed", null));
        await emails.SendOnceAsync(message.OrderId, "confirmed", message.CustomerEmail,
            "Your Bookstore order is confirmed",
            $"Order {message.OrderId} is confirmed. We will let you know when it ships.", ct);
    }

    public static async Task Handle(
        OrderCancelled message, IHubContext<OrderStatusHub, IOrderStatusClient> hub, IOrderEmails emails, CancellationToken ct)
    {
        await hub.Clients.User(message.CustomerId).OrderStatusChanged(new(message.OrderId, "Cancelled", message.Reason));
        await emails.SendOnceAsync(message.OrderId, "cancelled", message.CustomerEmail,
            "Your Bookstore order was cancelled",
            $"Order {message.OrderId} was cancelled: {message.Reason}. If you were charged, the amount will be refunded.", ct);
    }
}
