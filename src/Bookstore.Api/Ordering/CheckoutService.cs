using Bookstore.Payments;
using Bookstore.Shipping;

namespace Bookstore.Ordering;

public sealed record CheckoutResult(int OrderId, OrderStatus Status, decimal Total, string? ProviderPaymentId, DeclineReason? DeclineReason = null);

public sealed class CheckoutService(OrderStore orders, IShippingCarrier carrier, IPaymentGateway payments)
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
                return new CheckoutResult(order.Id, order.Status, total, approved.ProviderPaymentId);
            case PaymentResult.Declined declined:
                order.Status = OrderStatus.PaymentDeclined;
                return new CheckoutResult(order.Id, order.Status, total, null, declined.Reason);
            default:
                throw new InvalidOperationException($"Unknown payment result {result}.");
        }
    }
}
