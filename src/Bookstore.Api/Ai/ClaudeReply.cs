using Anthropic.Models.Messages;

namespace Bookstore.Api.Ai;

public enum ReplyOutcome
{
    Complete,
    Truncated,
    Refused,
    NeedsTools,
    Paused,
    ContextWindowFull,
    Unknown,
}

public sealed record TokenUsage(long Input, long Output, long CacheRead, long CacheWrite, long Thinking, long CacheWrite1h = 0);

public sealed record ClaudeReply(
    ReplyOutcome Outcome,
    string Text,
    string? RefusalCategory,
    TokenUsage Usage,
    IReadOnlyList<ContentBlockParam> AssistantContent);

public static class ClaudeReplyReader
{
    public static ClaudeReply Read(Message message)
    {
        ReplyOutcome outcome = message.StopReason?.Value() switch
        {
            StopReason.EndTurn or StopReason.StopSequence => ReplyOutcome.Complete,
            StopReason.MaxTokens => ReplyOutcome.Truncated,
            StopReason.Refusal => ReplyOutcome.Refused,
            StopReason.ToolUse => ReplyOutcome.NeedsTools,
            StopReason.PauseTurn => ReplyOutcome.Paused,
            StopReason.ModelContextWindowExceeded => ReplyOutcome.ContextWindowFull,
            _ => ReplyOutcome.Unknown,
        };

        // Never assume content[0] is text: thinking or tool_use blocks can come first.
        string text = string.Concat(message.Content
            .Select(block => block.TryPickText(out TextBlock? textBlock) ? textBlock.Text : string.Empty));

        Usage usage = message.Usage;
        TokenUsage tokens = new(
            usage.InputTokens,
            usage.OutputTokens,
            usage.CacheReadInputTokens ?? 0,
            usage.CacheCreationInputTokens ?? 0,
            usage.OutputTokensDetails?.ThinkingTokens ?? 0,
            usage.CacheCreation?.Ephemeral1hInputTokens ?? 0);

        // Echo the assistant turn back unchanged, thinking blocks included.
        List<ContentBlockParam> assistantContent = message.Content
            .Select(block => new ContentBlockParam(block.Json))
            .ToList();

        return new ClaudeReply(outcome, text.Trim(), message.StopDetails?.Category?.Raw(), tokens, assistantContent);
    }
}
