using Bookstore.Ordering;
using Microsoft.Extensions.Options;

namespace Bookstore.Payments;

public sealed class PaymentReconciliationOptions
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);

    // Give the in-flight request and the customer's own retry a chance first.
    public TimeSpan MinAge { get; init; } = TimeSpan.FromMinutes(2);

    // No charge at the provider after this long: the request never arrived. Must be well below the key retention.
    public TimeSpan AbandonAfter { get; init; } = TimeSpan.FromMinutes(30);
}

// Asks the provider about every payment whose outcome we do not know and settles the order.
public sealed class PaymentReconciliationJob(
    IServiceScopeFactory scopes,
    PaymentAttemptStore attempts,
    OrderStore orders,
    IOptions<PaymentReconciliationOptions> options,
    TimeProvider time,
    ILogger<PaymentReconciliationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad round must not stop the job; the next tick tries again.
                logger.LogError(ex, "Payment reconciliation round failed");
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        await using var scope = scopes.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();

        foreach (var attempt in attempts.OpenOlderThan(now - options.Value.MinAge))
        {
            var order = orders.Find(attempt.OrderId)!;
            attempt.LastCheckedAt = now;
            var charge = await payments.FindChargeAsync(attempt.Reference, cancellationToken);
            if (charge is not null)
            {
                attempt.Status = PaymentAttemptStatus.Succeeded;
                attempt.ProviderPaymentId = charge.ProviderPaymentId;
                order.Status = OrderStatus.Paid;
                order.ProviderPaymentId = charge.ProviderPaymentId;
                logger.LogInformation("Reconciled order {OrderId}: provider has charge {ProviderPaymentId}", order.Id, charge.ProviderPaymentId);
            }
            else if (now - attempt.CreatedAt >= options.Value.AbandonAfter)
            {
                attempt.Status = PaymentAttemptStatus.Abandoned;
                order.Status = OrderStatus.Placed;
                logger.LogWarning("Reconciled order {OrderId}: no charge after {Age}, attempt abandoned", order.Id, now - attempt.CreatedAt);
            }
            else
            {
                logger.LogInformation("Order {OrderId}: no charge at the provider yet, will check again", order.Id);
            }
        }
    }
}
