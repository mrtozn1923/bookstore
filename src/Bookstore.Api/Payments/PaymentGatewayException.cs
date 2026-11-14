namespace Bookstore.Payments;

public enum PaymentFailureKind
{
    // Retrying later may help (5xx, 429, connection refused).
    Transient,
    // The provider said "not now" for a long time (maintenance, circuit open).
    Unavailable,
    // The request may or may not have been processed (timeout after sending).
    UnknownOutcome,
    // The provider answered with something we cannot understand.
    ContractViolation
}

public sealed class PaymentGatewayException(PaymentFailureKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public PaymentFailureKind Kind { get; } = kind;

    public TimeSpan? RetryAfter { get; init; }
}
