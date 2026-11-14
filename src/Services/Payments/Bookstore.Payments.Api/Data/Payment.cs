using Bookstore.Contracts.Payments;

namespace Bookstore.Payments.Data;

public enum PaymentStatus
{
    Pending,
    Submitted,
    Unknown,
    Succeeded,
    Failed,
    Refunded
}

// One payment per order. Payments' own model: the provider's charge id is just a reference in it.
public sealed class Payment
{
    private Payment() { } // EF Core

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public PaymentStatus Status { get; private set; }
    public string? ProviderChargeId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Derived from the order, so every attempt for the same order sends the same key, wherever it runs.
    public string IdempotencyKey => $"charge-{OrderId}";

    public bool IsFinal => Status is PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Refunded;

    public static Payment Start(AuthorizePayment command, TimeProvider clock)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        return new Payment
        {
            Id = Guid.CreateVersion7(),
            OrderId = command.OrderId,
            Amount = command.Amount,
            Currency = command.Currency,
            Status = PaymentStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void MarkSubmitted(string chargeId, TimeProvider clock) => Move(PaymentStatus.Submitted, clock, chargeId);

    // The provider may or may not have charged the card: only a later lookup can tell.
    public void MarkUnknown(string reason, TimeProvider clock) => Move(PaymentStatus.Unknown, clock, failureReason: reason);

    // Returns the integration event for the saga, or null when the payment already had its final result.
    public object? Complete(bool succeeded, string chargeId, string? failureReason, TimeProvider clock)
    {
        if (IsFinal)
        {
            return null;
        }
        Move(succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed, clock, chargeId, failureReason);
        return succeeded
            ? new PaymentSucceeded(OrderId, chargeId)
            : new PaymentFailed(OrderId, failureReason ?? "unknown");
    }

    public void MarkRefunded(TimeProvider clock) => Move(PaymentStatus.Refunded, clock);

    private void Move(PaymentStatus status, TimeProvider clock, string? chargeId = null, string? failureReason = null)
    {
        Status = status;
        ProviderChargeId = chargeId ?? ProviderChargeId;
        FailureReason = failureReason;
        UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
    }
}

// Inbox for the provider's webhooks: the webhook-id header is the key.
public sealed class ProcessedWebhook
{
    public required string Id { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
}
