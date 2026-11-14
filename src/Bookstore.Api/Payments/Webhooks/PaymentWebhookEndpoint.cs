using Bookstore.Webhooks;
using Microsoft.Extensions.Options;

namespace Bookstore.Payments.Webhooks;

public sealed class PaymentWebhookOptions
{
    // Current secret first; during a rotation the previous one stays here for a while.
    public string[] Secrets { get; init; } = [];
    public TimeSpan Tolerance { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxBodyBytes { get; init; } = 64 * 1024;
}

public static class PaymentWebhookEndpoint
{
    // Verify -> store in the inbox -> queue -> 200. No business logic before the response.
    public static async Task<IResult> ReceiveAsync(
        HttpRequest request,
        PaymentWebhookInbox inbox,
        IOptions<PaymentWebhookOptions> options,
        TimeProvider time,
        ILogger<PaymentWebhookInbox> logger,
        CancellationToken cancellationToken)
    {
        // The raw bytes, exactly as sent: the signature covers these, not a re-serialized object.
        var body = await ReadBodyAsync(request, options.Value.MaxBodyBytes, cancellationToken);
        if (body is null)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var id = request.Headers["webhook-id"].ToString();
        var verification = WebhookSignature.Verify(id, request.Headers["webhook-timestamp"], request.Headers["webhook-signature"],
            body, options.Value.Secrets, time.GetUtcNow(), options.Value.Tolerance);
        if (verification != WebhookVerification.Valid)
        {
            logger.LogWarning("Rejected payment webhook {WebhookId}: {Reason}", id, verification);
            return Results.Unauthorized();
        }

        if (!inbox.TryAdd(new InboxMessage { Id = id, Body = body, ReceivedAt = time.GetUtcNow() }))
        {
            // A copy we already have: acknowledge it, otherwise the provider keeps resending.
            logger.LogInformation("Payment webhook {WebhookId} is a duplicate, acknowledged", id);
            return Results.Ok();
        }

        logger.LogInformation("Payment webhook {WebhookId} stored in the inbox", id);
        return Results.Ok();
    }

    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, int limit, CancellationToken cancellationToken)
    {
        if (request.ContentLength > limit)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
