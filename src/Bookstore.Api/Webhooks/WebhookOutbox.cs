namespace Bookstore.Webhooks;

public enum WebhookMessageStatus
{
    Pending,
    Delivered,
    Failed
}

public sealed record WebhookDeliveryAttempt(DateTimeOffset At, int? StatusCode, string? Error, TimeSpan Duration);

// One event for one subscription. Id is the webhook-id: the same on every attempt.
public sealed class WebhookMessage
{
    public required string Id { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required string EventType { get; init; }
    // Serialized once: every attempt sends and signs exactly these bytes.
    public required byte[] Payload { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public WebhookMessageStatus Status { get; set; } = WebhookMessageStatus.Pending;
    public DateTimeOffset NextAttemptAt { get; set; }
    // Failures since the last success or manual resend; picks the next delay from the schedule.
    public int ConsecutiveFailures { get; set; }
    public List<WebhookDeliveryAttempt> Attempts { get; } = [];
}

// In memory like the other stores; in the real Bookstore a WebhookMessages table written
// in the same transaction as the order change (Message Queue series, Transactional Outbox post).
public sealed class WebhookOutbox
{
    private readonly List<WebhookMessage> _messages = [];

    public void Add(WebhookMessage message)
    {
        lock (_messages)
        {
            _messages.Add(message);
        }
    }

    public IReadOnlyList<WebhookMessage> Due(DateTimeOffset now)
    {
        lock (_messages)
        {
            return _messages.Where(m => m.Status == WebhookMessageStatus.Pending && m.NextAttemptAt <= now).ToList();
        }
    }

    public WebhookMessage? Find(string id)
    {
        lock (_messages)
        {
            return _messages.SingleOrDefault(m => m.Id == id);
        }
    }

    public IReadOnlyList<WebhookMessage> All()
    {
        lock (_messages)
        {
            return _messages.ToList();
        }
    }
}
