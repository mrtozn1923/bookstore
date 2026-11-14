using Bookstore.Contracts.Payments;
using Bookstore.Payments.Data;
using Bookstore.Payments.Provider;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Payments.Checkout;

public static class PaymentCommandsHandler
{
    public static async Task<object?> Handle(
        AuthorizePayment command, PaymentsDbContext db, PaymentProviderClient provider, TimeProvider clock,
        PaymentMetrics metrics, ILogger logger, CancellationToken ct)
    {
        Payment? payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == command.OrderId, ct);
        if (payment is { Status: not PaymentStatus.Pending })
        {
            // A redelivered command: the charge already went out; its result comes through the webhook.
            logger.LogInformation("Payment for order {OrderId} is already {Status}", command.OrderId, payment.Status);
            return null;
        }
        if (payment is null)
        {
            payment = Payment.Start(command, clock);
            db.Payments.Add(payment);
        }

        ChargeOutcome outcome = await provider.ChargeAsync(payment.IdempotencyKey, command.OrderId, command.Amount, command.Currency, ct);
        switch (outcome)
        {
            case ChargeOutcome.Answered { State: ChargeState.Pending } answered:
                payment.MarkSubmitted(answered.ChargeId, clock);
                return null;
            case ChargeOutcome.Answered answered:
                object? paymentEvent = payment.Complete(answered.State == ChargeState.Succeeded, answered.ChargeId, answered.FailureReason, clock);
                metrics.Completed(paymentEvent, "answer");
                return paymentEvent;
            case ChargeOutcome.Unknown unknown:
                // Not a failure: the card may have been charged. Reconciliation finds out; the saga keeps waiting.
                metrics.UnknownResult();
                payment.MarkUnknown(unknown.Reason, clock);
                return null;
            default:
                throw new InvalidOperationException($"Unexpected outcome {outcome}");
        }
    }

    public static async Task Handle(
        RefundPayment command, PaymentsDbContext db, PaymentProviderClient provider, TimeProvider clock,
        ILogger logger, CancellationToken ct)
    {
        Payment? payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == command.OrderId, ct);
        if (payment is not { Status: PaymentStatus.Succeeded, ProviderChargeId: { } chargeId })
        {
            logger.LogInformation("Nothing to refund for order {OrderId} ({Status})", command.OrderId, payment?.Status);
            return;
        }

        // A refund is a new call with its own Idempotency-Key, not the charge undone.
        await provider.RefundAsync($"refund-{command.OrderId}", chargeId, ct);
        payment.MarkRefunded(clock);
        logger.LogInformation("Refunded {Amount} {Currency} for order {OrderId}: {Reason}",
            payment.Amount, payment.Currency, command.OrderId, command.Reason);
    }
}
