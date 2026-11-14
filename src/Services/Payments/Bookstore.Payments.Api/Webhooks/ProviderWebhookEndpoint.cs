using Bookstore.Payments.Checkout;
using System.Text.Json;
using Bookstore.Payments.Data;
using Bookstore.Payments.Provider;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Bookstore.Payments.Webhooks;

public static class ProviderWebhookEndpoint
{
    public static IEndpointRouteBuilder MapProviderWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/payment-provider", HandleAsync);
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request, WebhookSignature signature, IDbContextOutbox<PaymentsDbContext> outbox,
        TimeProvider clock, PaymentMetrics metrics, ILogger<WebhookSignature> logger, CancellationToken ct)
    {
        // The signature covers the exact bytes: read the raw body before any JSON parsing.
        string body = await new StreamReader(request.Body).ReadToEndAsync(ct);
        if (!signature.IsValid(request.Headers, body, out string error))
        {
            logger.LogWarning("Rejected webhook {WebhookId}: {Error}", request.Headers["webhook-id"].ToString(), error);
            return Results.Problem(title: "Invalid webhook signature", statusCode: StatusCodes.Status401Unauthorized);
        }

        string webhookId = request.Headers["webhook-id"].ToString();
        PaymentsDbContext db = outbox.DbContext;
        if (await db.ProcessedWebhooks.AnyAsync(w => w.Id == webhookId, ct))
        {
            // Providers redeliver until they get a 2xx. A repeat gets 200 and changes nothing.
            logger.LogInformation("Webhook {WebhookId} already processed", webhookId);
            return Results.Ok();
        }

        ProviderWebhook webhook = JsonSerializer.Deserialize<ProviderWebhook>(body, ProviderJson.Options)!;
        ChargeOutcome.Answered result = PaymentProviderClient.ToOutcome(webhook.Data);
        Payment? payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == Guid.Parse(webhook.Data.Reference), ct);
        if (payment is null || result.State == ChargeState.Pending)
        {
            return Results.Ok();
        }

        db.ProcessedWebhooks.Add(new ProcessedWebhook { Id = webhookId, ReceivedAtUtc = clock.GetUtcNow().UtcDateTime });
        if (payment.Complete(result.State == ChargeState.Succeeded, result.ChargeId, result.FailureReason, clock) is { } paymentEvent)
        {
            await outbox.PublishAsync(paymentEvent);
            metrics.Completed(paymentEvent, "webhook");
        }

        try
        {
            // Inbox row, payment status and the saga's event in one transaction.
            await outbox.SaveChangesAndFlushMessagesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            // The same webhook arrived twice at the same time; the other request processed it.
            return Results.Ok();
        }

        logger.LogInformation("Webhook {WebhookId}: payment for order {OrderId} is {Status}", webhookId, payment.OrderId, payment.Status);
        return Results.Ok();
    }
}
