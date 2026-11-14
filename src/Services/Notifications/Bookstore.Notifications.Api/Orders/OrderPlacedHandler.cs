using Bookstore.Contracts.Ordering;

namespace Bookstore.Notifications.Orders;

// For now only a log line; the e-mail and the live status push come in post 11.
public static class OrderPlacedHandler
{
    public static void Handle(OrderPlaced message, ILogger logger) =>
        logger.LogInformation("Order {OrderId} placed by {CustomerEmail}: {Total} {Currency}, {Lines} line(s)",
            message.OrderId, message.CustomerEmail, message.Total, message.Currency, message.Lines.Count);
}
