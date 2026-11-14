using System.Diagnostics;
using Bookstore.Observability;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Bookstore.Notifications;

public sealed class OrderConfirmationSender(OrderConfirmationQueue queue, ILogger<OrderConfirmationSender> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (OrderConfirmationMessage message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            PropagationContext parent = Propagators.DefaultTextMapPropagator.Extract(default, message.Headers,
                static (carrier, key) => carrier.TryGetValue(key, out string? value) ? [value] : []);
            Baggage.Current = parent.Baggage;

            // The consumer span continues the checkout's trace: its parent is the producer span in the message.
            using Activity? activity = BookstoreActivitySources.Notifications.StartActivity(
                "order-confirmations process", ActivityKind.Consumer, parent.ActivityContext);
            activity?.SetTag("messaging.system", "in-process");
            activity?.SetTag("messaging.destination.name", "order-confirmations");
            activity?.SetTag("bookstore.order.id", message.OrderId);

            // Stands in for the SMTP call (Notifications module).
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(20, 60)), stoppingToken);
            logger.LogInformation("Order confirmation for order {OrderId} sent to {CustomerEmail}", message.OrderId, message.CustomerEmail);
        }
    }
}

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services) =>
        services.AddSingleton<OrderConfirmationQueue>()
            .AddHostedService<OrderConfirmationSender>();
}
