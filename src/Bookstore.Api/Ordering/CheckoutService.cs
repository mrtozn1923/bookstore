using Bookstore.Notifications;
using Bookstore.Payments;
using Bookstore.Shipping;

namespace Bookstore.Ordering;

public sealed record CheckoutResult(int OrderId, OrderStatus Status, decimal Total, string? ProviderPaymentId, DeclineReason? DeclineReason = null)
{
    public bool ShippingIsFlatRate { get; init; }
}

public sealed class CheckoutService(
    OrderStore orders,
    IShippingCarrier carrier,
    IPaymentGateway payments,
    IEmailSender email,
    ILogger<CheckoutService> logger)
{
    private const string WarehousePostalCode = "34000";

    public async Task<CheckoutResult?> CheckoutAsync(int orderId, string cardToken, CancellationToken cancellationToken)
    {
        var order = orders.Find(orderId);
        if (order is null)
        {
            return null;
        }

        var quote = await carrier.GetQuoteAsync(
            new ShippingQuoteRequest(WarehousePostalCode, order.ShipToPostalCode, order.WeightGrams), cancellationToken);
        order.ShippingPrice = quote.Price;
        var total = order.ItemsTotal + quote.Price;

        order.Status = OrderStatus.PaymentPending;
        var result = await payments.ChargeAsync(new PaymentRequest(order.Id, total, order.Currency, cardToken), cancellationToken);

        switch (result)
        {
            case PaymentResult.Approved approved:
                order.Status = OrderStatus.Paid;
                order.ProviderPaymentId = approved.ProviderPaymentId;
                await SendConfirmationAsync(order, total, cancellationToken);
                return new CheckoutResult(order.Id, order.Status, total, approved.ProviderPaymentId)
                    { ShippingIsFlatRate = quote.IsFlatRate };
            case PaymentResult.Declined declined:
                order.Status = OrderStatus.PaymentDeclined;
                return new CheckoutResult(order.Id, order.Status, total, null, declined.Reason)
                    { ShippingIsFlatRate = quote.IsFlatRate };
            default:
                throw new InvalidOperationException($"Unknown payment result {result}.");
        }
    }

    // The order is paid; a failed e-mail must not turn that into an error for the customer.
    private async Task SendConfirmationAsync(Order order, decimal total, CancellationToken cancellationToken)
    {
        try
        {
            await email.SendAsync(order.CustomerEmail, $"Bookstore order {order.Id} confirmed",
                $"Hello {order.CustomerName}, we received {total:0.00} {order.Currency}.", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Order {OrderId} confirmation e-mail could not be sent", order.Id);
        }
    }
}
