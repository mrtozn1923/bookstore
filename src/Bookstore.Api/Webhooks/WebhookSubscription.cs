namespace Bookstore.Webhooks;

public enum WebhookSubscriptionStatus
{
    Enabled,
    Disabled
}

public sealed record WebhookSecret(string Value, DateTimeOffset? ExpiresAt);

// An institutional customer's endpoint and the events it wants.
public sealed class WebhookSubscription
{
    public required Guid Id { get; init; }
    public required string InstitutionId { get; init; }
    public required Uri Url { get; init; }
    public required string[] EventTypes { get; init; }
    public List<WebhookSecret> Secrets { get; } = [];
    public WebhookSubscriptionStatus Status { get; set; } = WebhookSubscriptionStatus.Enabled;
    public string? DisabledReason { get; set; }
    // First failure of the current failure streak; null after a success.
    public DateTimeOffset? FailingSince { get; set; }

    public IEnumerable<string> ActiveSecrets(DateTimeOffset now) =>
        Secrets.Where(s => s.ExpiresAt is null || s.ExpiresAt > now).Select(s => s.Value);
}

public sealed class WebhookSubscriptionStore
{
    private readonly List<WebhookSubscription> _subscriptions = [];

    public WebhookSubscription Add(string institutionId, Uri url, string[] eventTypes)
    {
        var subscription = new WebhookSubscription { Id = Guid.CreateVersion7(), InstitutionId = institutionId, Url = url, EventTypes = eventTypes };
        subscription.Secrets.Add(new WebhookSecret(WebhookSignature.NewSecret(), null));
        lock (_subscriptions)
        {
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    public WebhookSubscription? Find(Guid id)
    {
        lock (_subscriptions)
        {
            return _subscriptions.SingleOrDefault(s => s.Id == id);
        }
    }

    public IReadOnlyList<WebhookSubscription> For(string institutionId, string eventType)
    {
        lock (_subscriptions)
        {
            return _subscriptions
                .Where(s => s.InstitutionId == institutionId && s.Status == WebhookSubscriptionStatus.Enabled && s.EventTypes.Contains(eventType))
                .ToList();
        }
    }
}
