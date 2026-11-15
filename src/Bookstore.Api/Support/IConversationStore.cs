using System.Collections.Concurrent;
using Anthropic.Models.Messages;

namespace Bookstore.Api.Support;

public interface IConversationStore
{
    IReadOnlyList<MessageParam> Get(Guid conversationId);
    void Save(Guid conversationId, IReadOnlyList<MessageParam> messages);
}

// The Messages API is stateless: the whole history is sent on every request.
// In production this lives in a database; here it stays in memory.
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<MessageParam>> _conversations = new();

    public IReadOnlyList<MessageParam> Get(Guid conversationId) =>
        _conversations.TryGetValue(conversationId, out IReadOnlyList<MessageParam>? messages) ? messages : [];

    public void Save(Guid conversationId, IReadOnlyList<MessageParam> messages) =>
        _conversations[conversationId] = messages;
}
