using System.Globalization;
using Bookstore.Notifications;

namespace Bookstore.Payments;

public enum PaymentOutcome { Paid, Declined, Blocked }

public sealed class PaymentService(
    IPaymentGatewayFactory gateways, IFraudCheck fraud, IPaymentAuditLog audit, INotificationSender notifications)
{
    public const int FraudThreshold = 80;

    public async Task<PaymentOutcome> PayAsync(
        Guid orderId, decimal amount, string currency, string email, string cardToken, CancellationToken ct)
    {
        if (fraud.TryScore(email, amount, out int score) && score >= FraudThreshold)
        {
            audit.Write($"blocked {orderId} score={score}");
            return PaymentOutcome.Blocked;
        }

        IPaymentGateway gateway = gateways.For(currency);
        ChargeRequest request = new(orderId, amount, currency, cardToken);
        ChargeResult result;
        try
        {
            result = await gateway.ChargeAsync(request, ct);
        }
        catch (TimeoutException)
        {
            audit.Write($"timeout {orderId} via {gateway.ProviderName}, retrying");
            result = await gateway.ChargeAsync(request, ct);
        }

        if (!result.Approved)
        {
            audit.Write($"declined {orderId} {result.DeclineCode}");
            return PaymentOutcome.Declined;
        }

        audit.Write($"charged {orderId} {result.TransactionId}");
        string body = string.Create(CultureInfo.InvariantCulture, $"We received {amount:0.00} {currency} for your order.");
        await notifications.SendAsync(new EmailMessage(email, "Payment received", body), ct);
        return PaymentOutcome.Paid;
    }
}
