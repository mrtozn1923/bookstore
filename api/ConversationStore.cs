using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace Bookstore.Assistant.Api;

// Demo için bellekte; kalıcı saklama ve sahiplik kontrolü .NET ile Yapay Zeka serisinin 5. yazısında
public sealed class ConversationStore
{
    private readonly ConcurrentDictionary<Guid, List<ChatMessage>> _items = new();

    public (Guid Id, List<ChatMessage> History) GetOrCreate(Guid? id)
    {
        if (id is Guid existing && _items.TryGetValue(existing, out List<ChatMessage>? history))
        {
            return (existing, history);
        }
        Guid created = Guid.NewGuid();
        return (created, _items.GetOrAdd(created, _ => []));
    }

    public List<ChatMessage>? Find(Guid id) => _items.TryGetValue(id, out List<ChatMessage>? history) ? history : null;
}
