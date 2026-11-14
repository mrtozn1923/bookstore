using Bookstore.Institutional.Application;
using Bookstore.Institutional.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookstore.Institutional.Infrastructure;

internal sealed class OrderApprovedConsumer(
    ChannelEventBus bus,
    IServiceScopeFactory scopes,
    INotificationSender mail,
    ILogger<OrderApprovedConsumer> logger) : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (OrderApproved e in bus.Reader.ReadAllAsync(stoppingToken))
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
            IDistributorGateway distributor = scope.ServiceProvider.GetRequiredService<IDistributorGateway>();
            Order order = await db.Orders
                .Include(o => o.PlacedBy).ThenInclude(u => u.InstitutionUnit).ThenInclude(u => u.PurchasingManager)
                .SingleAsync(o => o.Id == e.Id, stoppingToken);

            // par: dağıtıcıya iletme ve e-posta birbirini beklemez
            Task distributorTask = e.NewStatus == OrderStatus.Approved
                ? distributor.PlaceOrderAsync(order, stoppingToken)
                : Task.CompletedTask;
            Task mailTask = SendWithRetryAsync(order.PlacedBy.Email,
                $"Order {order.Id}: {e.NewStatus}", stoppingToken);
            await Task.WhenAll(distributorTask, mailTask);

            // opt: satın alma müdürü onayına düştüyse ona da haber ver (R10)
            if (e.NewStatus == OrderStatus.PendingPurchasingManager)
                await SendWithRetryAsync(order.PlacedBy.InstitutionUnit.PurchasingManager!.Email,
                    $"Order {order.Id} waits for your approval", stoppingToken);
        }
    }

    private async Task SendWithRetryAsync(string to, string subject, CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(AttemptTimeout);
            try
            {
                await mail.SendAsync(to, subject, "", timeout.Token);
                return;
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Mail attempt {Attempt} timed out after {Timeout}", attempt, AttemptTimeout);
            }
        }
        logger.LogError("Mail to {To} failed after {Max} attempts", to, MaxAttempts);
    }
}
