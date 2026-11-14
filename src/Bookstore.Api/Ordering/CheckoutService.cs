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
    PaymentAttemptStore attempts,
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

        if (order.Status == OrderStatus.Paid)
        {
            // A second click on "Pay" must not become a second charge.
            return new CheckoutResult(order.Id, order.Status, order.ItemsTotal + order.ShippingPrice!.Value, order.ProviderPaymentId);
        }

        var quote = await carrier.GetQuoteAsync(
            new ShippingQuoteRequest(WarehousePostalCode, order.ShipToPostalCode, order.WeightGrams), cancellationToken);
        order.ShippingPrice = quote.Price;
        var total = order.ItemsTotal + quote.Price;

        // The key is created once per payment intent and stored before the first request leaves.
        var attempt = attempts.GetOrStart(order.Id, total, order.Currency, cardToken, out var reused);
        if (reused)
        {
            logger.LogInformation("Order {OrderId}: resending payment with the stored key {IdempotencyKey}", order.Id, attempt.IdempotencyKey);
        }

        order.Status = OrderStatus.PaymentPending;
        PaymentResult result;
        try
        {
            result = await payments.ChargeAsync(
                new PaymentRequest(order.Id, total, order.Currency, cardToken, attempt.IdempotencyKey, attempt.Reference), cancellationToken);
        }
        catch (PaymentGatewayException ex) when (ex.Kind == PaymentFailureKind.UnknownOutcome)
        {
            // Maybe charged, maybe not: keep the attempt open; a retry or reconciliation will find out.
            logger.LogWarning("Order {OrderId}: payment outcome unknown, key {IdempotencyKey}", order.Id, attempt.IdempotencyKey);
            return new CheckoutResult(order.Id, order.Status, total, null) { ShippingIsFlatRate = quote.IsFlatRate };
        }

        switch (result)
        {
            case PaymentResult.Approved approved:
                attempt.Status = PaymentAttemptStatus.Succeeded;
                attempt.ProviderPaymentId = approved.ProviderPaymentId;
                order.Status = OrderStatus.Paid;
                order.ProviderPaymentId = approved.ProviderPaymentId;
                await SendConfirmationAsync(order, total, cancellationToken);
                return new CheckoutResult(order.Id, order.Status, total, approved.ProviderPaymentId)
                    { ShippingIsFlatRate = quote.IsFlatRate };
            case PaymentResult.Declined declined:
                attempt.Status = PaymentAttemptStatus.Declined;
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
