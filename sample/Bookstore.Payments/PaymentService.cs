namespace Bookstore.Payments;

public sealed class PaymentService(ILogger<PaymentService> logger, IPaymentProvider provider)
{
    public async Task<bool> AuthorizeAsync(int paymentId, decimal amount, CancellationToken ct)
    {
        bool authorized = await provider.AuthorizeAsync(paymentId, amount, ct);
        if (authorized)
        {
            logger.LogInformation($"Payment {paymentId} authorized");
            return true;
        }

        logger.LogError(
            $"Payment {paymentId} declined");
        return false;
    }

    public void RequestRefund(string orderNumber) =>
        logger.LogInformation("Refund requested for {OrderNumber}", orderNumber);
}
