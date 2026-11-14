namespace Bookstore.Assistant.Api;

// ReplaceLastTurn: sunucudaki son kullanıcı + asistan çiftini sil (yeniden üret / düzenle)
public sealed record ChatStreamRequest(string Message, Guid? ConversationId = null, bool ReplaceLastTurn = false);

// /chat/sse event sözlüğü: Angular tarafındaki chat-events.ts ile birebir aynı alanlar
public sealed record ConversationEvent(Guid Id);
public sealed record DeltaEvent(string Text);
public sealed record ToolEvent(string CallId, string Name, string Phase);
public sealed record UsageEvent(long? InputTokens, long? OutputTokens);
public sealed record ErrorEvent(string Message, bool Partial);
public sealed record ApprovalEvent(string ApprovalId, string Tool, IDictionary<string, object?>? Arguments, int ExpiresInSeconds);
public sealed record UiEvent(string CallId, string Component, object? Props);

public sealed record ApprovalDecision(bool Approved, string? Reason = null);

public sealed record HistoryItem(string Role, string Text);
