namespace Bookstore.Contracts.Payments;

public sealed record AuthorizePayment(Guid OrderId, decimal Amount, string Currency, string CustomerEmail);

// A refund is a new payment operation, not an "undo" of the charge.
public sealed record RefundPayment(Guid OrderId, string Reason);

public sealed record PaymentSucceeded(Guid OrderId, string PaymentReference);

public sealed record PaymentFailed(Guid OrderId, string Reason);
