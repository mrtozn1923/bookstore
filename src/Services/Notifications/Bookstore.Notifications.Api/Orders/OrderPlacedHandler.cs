using Bookstore.Contracts.Ordering;

namespace Bookstore.Notifications.Orders;

// For now only log lines; the e-mail and the live status push come in post 11.
public static class OrderPlacedHandler
{
    public static void Handle(OrderPlaced message, ILogger logger) =>
        logger.LogInformation("Order {OrderId} placed by {CustomerEmail}: {Total} {Currency}, {Lines} line(s)",
            message.OrderId, message.CustomerEmail, message.Total, message.Currency, message.Lines.Count);

    public static void Handle(OrderConfirmed message, ILogger logger) =>
        logger.LogInformation("Order {OrderId} confirmed for {CustomerEmail}", message.OrderId, message.CustomerEmail);

    public static void Handle(OrderCancelled message, ILogger logger) =>
        logger.LogInformation("Order {OrderId} cancelled for {CustomerEmail}: {Reason}",
            message.OrderId, message.CustomerEmail, message.Reason);
}
