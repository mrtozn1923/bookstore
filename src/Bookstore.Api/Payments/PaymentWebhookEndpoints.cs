using Bookstore.Api.Options;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Payments;

public static class PaymentWebhookEndpoints
{
    public static void MapPaymentWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/webhook", async (
            HttpRequest request,
            IOptionsMonitor<PaymentWebhookOptions> options,
            ILoggerFactory loggerFactory) =>
        {
            using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer);

            // IOptionsMonitor: rotation'da yeni secret restart olmadan görünür
            var current = options.CurrentValue;
            var match = WebhookSignature.Verify(
                request.Headers["X-Bookstore-Signature"], buffer.ToArray(), current.Secret, current.SecondarySecret);

            if (match == WebhookSecretMatch.None)
            {
                return Results.Unauthorized();
            }

            // Hangi secret'ın kullanıldığı, eskisini ne zaman kaldırabileceğini söyler
            loggerFactory.CreateLogger("Bookstore.Payments.Webhook").LogInformation(
                "Payment webhook verified with {SecretSlot} secret", match);
            return Results.Ok(new { Accepted = true });
        });
    }
}
