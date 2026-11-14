using Bookstore.Payments.Checkout;
using Bookstore.Payments.Data;
using Bookstore.Payments.Provider;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Bookstore.Payments.Reconciliation;

public sealed class ReconciliationOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
    // A submitted payment gets this long for its webhook before we ask the provider ourselves.
    public TimeSpan MinAge { get; set; } = TimeSpan.FromMinutes(2);
}

// Settles payments whose result we do not know: a timed-out charge, or a webhook that never came.
public sealed class PaymentReconciliationJob(
    IServiceScopeFactory scopes, TimeProvider clock, Microsoft.Extensions.Options.IOptions<ReconciliationOptions> options,
    PaymentMetrics metrics, ILogger<PaymentReconciliationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.Interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Reconciliation failed; trying again on the next tick");
            }
        }
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        DateTime cutoff = clock.GetUtcNow().UtcDateTime - options.Value.MinAge;
        List<Guid> orderIds;
        await using (AsyncServiceScope scope = scopes.CreateAsyncScope())
        {
            PaymentsDbContext db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            orderIds = await db.Payments
                .Where(p => (p.Status == PaymentStatus.Unknown || p.Status == PaymentStatus.Submitted) && p.UpdatedAtUtc <= cutoff)
                .OrderBy(p => p.UpdatedAtUtc)
                .Select(p => p.OrderId)
                .Take(50)
                .ToListAsync(ct);
        }

        foreach (Guid orderId in orderIds)
        {
            // One scope and one transaction per payment: one bad payment does not block the others.
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            IDbContextOutbox<PaymentsDbContext> outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox<PaymentsDbContext>>();
            PaymentProviderClient provider = scope.ServiceProvider.GetRequiredService<PaymentProviderClient>();
            Payment payment = await outbox.DbContext.Payments.SingleAsync(p => p.OrderId == orderId, ct);

            ChargeOutcome.Answered? known = await provider.FindByOrderAsync(orderId, ct);
            ChargeOutcome outcome = known ?? await provider.ChargeAsync(payment.IdempotencyKey, orderId, payment.Amount, payment.Currency, ct);
            logger.LogInformation("Reconciling order {OrderId} ({Status}): provider says {Outcome}",
                orderId, payment.Status, known is null ? $"no charge, submitted again: {outcome}" : outcome.ToString());

            switch (outcome)
            {
                case ChargeOutcome.Answered { State: ChargeState.Pending } pending:
                    payment.MarkSubmitted(pending.ChargeId, clock);
                    break;
                case ChargeOutcome.Answered final when payment.Complete(
                    final.State == ChargeState.Succeeded, final.ChargeId, final.FailureReason, clock) is { } paymentEvent:
                    await outbox.PublishAsync(paymentEvent);
                    metrics.Completed(paymentEvent, "reconciliation");
                    break;
            }
            await outbox.SaveChangesAndFlushMessagesAsync(ct);
        }
    }
}
