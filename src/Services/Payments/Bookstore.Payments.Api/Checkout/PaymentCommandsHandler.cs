using Bookstore.Contracts.Payments;

namespace Bookstore.Payments.Checkout;

// Stand-in until post 08 connects a (fake) payment provider: amounts above a limit are declined.
public static class PaymentCommandsHandler
{
    public static object Handle(AuthorizePayment command, IConfiguration configuration, ILogger logger)
    {
        decimal declineAbove = configuration.GetValue("Payments:DeclineAbove", 5000m);
        if (command.Amount > declineAbove)
        {
            logger.LogInformation("Declined {Amount} {Currency} for order {OrderId}", command.Amount, command.Currency, command.OrderId);
            return new PaymentFailed(command.OrderId, "card-declined");
        }

        string reference = $"pay_{Guid.CreateVersion7():N}";
        logger.LogInformation("Authorized {Amount} {Currency} for order {OrderId}: {Reference}",
            command.Amount, command.Currency, command.OrderId, reference);
        return new PaymentSucceeded(command.OrderId, reference);
    }

    public static void Handle(RefundPayment command, ILogger logger) =>
        logger.LogWarning("Refund requested for order {OrderId}: {Reason}", command.OrderId, command.Reason);
}
