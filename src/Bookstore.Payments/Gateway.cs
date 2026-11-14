namespace Bookstore.Payments;

public sealed record ChargeRequest(Guid OrderId, decimal Amount, string Currency, string CardToken);

public sealed record ChargeResult(bool Approved, string? TransactionId, string? DeclineCode);

public sealed class PaymentSettledEventArgs(string transactionId, decimal amount) : EventArgs
{
    public string TransactionId { get; } = transactionId;
    public decimal Amount { get; } = amount;
}

public interface IPaymentGateway
{
    string ProviderName { get; }

    Task<ChargeResult> ChargeAsync(ChargeRequest request, CancellationToken ct);

    Task RefundAsync(string transactionId, decimal amount, CancellationToken ct);

    event EventHandler<PaymentSettledEventArgs>? Settled;
}

public interface IPaymentGatewayFactory
{
    IPaymentGateway For(string currency);
}

public interface IFraudCheck
{
    bool TryScore(string email, decimal amount, out int score);
}

public interface IPaymentAuditLog
{
    void Write(string entry);
}
