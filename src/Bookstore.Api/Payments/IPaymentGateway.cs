namespace Bookstore.Payments;

// The port: Bookstore's own language for taking a payment.
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken);

    // Reconciliation: what does the provider know about this reference? null = no charge.
    Task<PaymentResult.Approved?> FindChargeAsync(string reference, CancellationToken cancellationToken);
}

public sealed record PaymentRequest(
    int OrderId, decimal Amount, string Currency, string CardToken, string IdempotencyKey, string Reference);

public abstract record PaymentResult
{
    public sealed record Approved(string ProviderPaymentId) : PaymentResult;

    public sealed record Declined(DeclineReason Reason) : PaymentResult;
}

public enum DeclineReason
{
    CardDeclined,
    InsufficientFunds,
    ExpiredCard,
    Other
}
