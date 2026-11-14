using System.Threading.Channels;

namespace Bookstore.Payments.Webhooks;

public enum InboxStatus
{
    Received,
    Processed,
    Failed
}

public sealed class InboxMessage
{
    public required string Id { get; init; }
    public required byte[] Body { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public InboxStatus Status { get; set; } = InboxStatus.Received;
    public int Attempts { get; set; }
    public string? Error { get; set; }
}

// webhook-id is the key: the second copy of an event is recognized here, before any processing.
// In memory in this sample; in Bookstore a WebhookInbox table with the id as primary key.
public sealed class PaymentWebhookInbox
{
    private readonly Dictionary<string, InboxMessage> _messages = [];
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();

    public ChannelReader<string> Queue => _queue.Reader;

    public bool TryAdd(InboxMessage message)
    {
        lock (_messages)
        {
            if (!_messages.TryAdd(message.Id, message))
            {
                return false;
            }
        }

        _queue.Writer.TryWrite(message.Id);
        return true;
    }

    public InboxMessage? Find(string id)
    {
        lock (_messages)
        {
            return _messages.GetValueOrDefault(id);
        }
    }
}
