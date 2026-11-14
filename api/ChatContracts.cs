namespace Bookstore.Assistant.Api;

public sealed record ChatStreamRequest(string Message);

// /chat/sse event sözlüğü: Angular tarafındaki chat-events.ts ile birebir aynı alanlar
public sealed record DeltaEvent(string Text);
public sealed record ToolEvent(string CallId, string Name, string Phase);
public sealed record UsageEvent(long? InputTokens, long? OutputTokens);
public sealed record ErrorEvent(string Message, bool Partial);
