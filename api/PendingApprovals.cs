using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace Bookstore.Assistant.Api;

public sealed record PendingApproval(
    ToolApprovalRequestContent Request, Guid ConversationId, List<ChatMessage> History,
    ChatMessage UserMessage, List<ChatMessage> Messages, DateTimeOffset ExpiresAt);

// Onay bekleyen turlar; süresi dolan onay kabul edilmez. Demo için bellekte.
public sealed class PendingApprovals(TimeProvider time)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<string, PendingApproval> _items = new();

    public void Save(ToolApprovalRequestContent request, Guid conversationId, List<ChatMessage> history,
        ChatMessage userMessage, List<ChatMessage> messages) =>
        _items[request.RequestId] = new(request, conversationId, history, userMessage, messages, time.GetUtcNow() + Lifetime);

    // Bir onay yalnız bir kez ve süresi içinde kullanılabilir
    public PendingApproval? Take(string requestId) =>
        _items.TryRemove(requestId, out PendingApproval? item) && item.ExpiresAt > time.GetUtcNow() ? item : null;
}
