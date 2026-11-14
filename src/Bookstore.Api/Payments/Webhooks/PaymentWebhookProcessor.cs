using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.Payments.Webhooks;

// Processes inbox messages after the 200 has been sent. Every step is safe to repeat.
public sealed class PaymentWebhookProcessor(
    PaymentWebhookInbox inbox,
    PaymentAttemptStore attempts,
    OrderStore orders,
    IServiceScopeFactory scopes,
    ILogger<PaymentWebhookProcessor> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in inbox.Queue.ReadAllAsync(stoppingToken))
        {
            var message = inbox.Find(id)!;
            message.Attempts++;
            try
            {
                await ProcessAsync(message, stoppingToken);
                message.Status = InboxStatus.Processed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Stays in the inbox as Failed; reconciliation still covers the payment itself.
                message.Status = InboxStatus.Failed;
                message.Error = ex.Message;
                logger.LogError(ex, "Payment webhook {WebhookId} could not be processed", id);
            }
        }
    }

    public async Task ProcessAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<PaymentEvent>(message.Body, Json)!;
        if (evt.Type != "payment.succeeded")
        {
            logger.LogInformation("Ignoring payment webhook of type {Type}", evt.Type);
            return;
        }

        var attempt = attempts.FindByReference(evt.Data.MerchantReference);
        if (attempt is null)
        {
            throw new InvalidOperationException($"No payment attempt for reference {evt.Data.MerchantReference}.");
        }

        if (attempt.Status == PaymentAttemptStatus.Succeeded)
        {
            // Checkout, a retry or reconciliation got there first: nothing to do.
            logger.LogInformation("Order {OrderId} is already paid, webhook {WebhookId} changes nothing", attempt.OrderId, message.Id);
            return;
        }

        // The signature says who sent it, not that it is right: ask the provider for the current state.
        await using var scope = scopes.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var charge = await gateway.FindChargeAsync(attempt.Reference, cancellationToken);
        if (charge?.ProviderPaymentId != evt.Data.Id || evt.Data.Amount != (long)(attempt.Amount * 100))
        {
            throw new InvalidOperationException($"Webhook {message.Id} does not match the provider's charge for {attempt.Reference}.");
        }

        var order = orders.Find(attempt.OrderId)!;
        attempt.Status = PaymentAttemptStatus.Succeeded;
        attempt.ProviderPaymentId = evt.Data.Id;
        order.Status = OrderStatus.Paid;
        order.ProviderPaymentId = evt.Data.Id;
        logger.LogInformation("Order {OrderId} paid by webhook {WebhookId} ({ProviderPaymentId})", order.Id, message.Id, evt.Data.Id);
    }

    private sealed record PaymentEvent(string Type, DateTimeOffset Timestamp, PaymentEventData Data);

    private sealed record PaymentEventData(string Id, string Status, long Amount, string Currency, string MerchantReference);
}
