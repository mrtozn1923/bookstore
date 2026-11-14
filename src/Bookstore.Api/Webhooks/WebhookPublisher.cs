using System.Text.Json;

namespace Bookstore.Webhooks;

// Event catalog: the type names and payloads institutional customers can subscribe to.
public static class WebhookEventTypes
{
    public const string OrderShipped = "order.shipped";
}

public sealed record OrderShippedData(int OrderId, string ShipmentId, string TrackingNumber);

// Writes one outbox message per subscription; the dispatcher delivers them later.
public sealed class WebhookPublisher(WebhookSubscriptionStore subscriptions, WebhookOutbox outbox, TimeProvider time)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public int Publish<TData>(string institutionId, string eventType, TData data)
    {
        var now = time.GetUtcNow();
        var targets = subscriptions.For(institutionId, eventType);
        foreach (var subscription in targets)
        {
            // Standard Webhooks payload: type, timestamp (when it happened) and data
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { type = eventType, timestamp = now, data }, Json);
            outbox.Add(new WebhookMessage
            {
                Id = $"msg_{Guid.CreateVersion7(now):N}",
                SubscriptionId = subscription.Id,
                EventType = eventType,
                Payload = payload,
                CreatedAt = now,
                NextAttemptAt = now
            });
        }

        return targets.Count;
    }
}
