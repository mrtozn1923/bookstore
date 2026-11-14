namespace Bookstore.Payments;

// The port: Bookstore's own language for taking a payment.
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken);
}

public sealed record PaymentRequest(int OrderId, decimal Amount, string Currency, string CardToken);

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
