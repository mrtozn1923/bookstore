using System.Collections.Concurrent;
using Anthropic.Models.Messages;

namespace Bookstore.Api.Support;

public sealed record Conversation(IReadOnlyList<MessageParam> Messages, string? LastMessageId)
{
    public static Conversation Empty { get; } = new([], null);
}

public interface IConversationStore
{
    Conversation Get(Guid conversationId);
    void Save(Guid conversationId, Conversation conversation);
}

// The Messages API is stateless: the whole history is sent on every request.
// In production this lives in a database; here it stays in memory.
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, Conversation> _conversations = new();

    public Conversation Get(Guid conversationId) =>
        _conversations.TryGetValue(conversationId, out Conversation? conversation) ? conversation : Conversation.Empty;

    public void Save(Guid conversationId, Conversation conversation) =>
        _conversations[conversationId] = conversation;
}
