using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Support;

public sealed record SupportAnswer(string Text, ReplyOutcome Outcome, TokenUsage Usage);

public sealed class SupportChatService(
    IAnthropicClient claude,
    IConversationStore conversations,
    SupportKnowledge knowledge,
    ClaudeUsageMeter usageMeter,
    IOptions<ClaudeOptions> options,
    ILogger<SupportChatService> logger)
{
    public const string SystemPrompt = """
        You are the customer support assistant of Bookstore, an online bookstore.
        Answer in the customer's language. Keep answers under 120 words.
        You can explain orders, shipping, returns and payments in general terms.
        You cannot see order data yet; if the customer needs a specific order checked, say that a team member will follow up.
        Use the support policy below as the only source for rules, prices and deadlines.
        """;

    // Room for adaptive thinking plus a 120-word answer; thinking counts toward max_tokens.
    public const int MaxTokens = 2048;

    private const string RefusalText = "I can't help with this request here. A member of our support team will contact you.";

    public static MessageCreateParams BuildRequest(
        IReadOnlyList<MessageParam> history,
        SupportKnowledge knowledge,
        ClaudeOptions settings,
        string? previousMessageId = null)
    {
        MessageCreateParams request = new()
        {
            Model = settings.Model,
            MaxTokens = MaxTokens,
            System = new List<TextBlockParam>
            {
                new() { Text = SystemPrompt },
                // Breakpoint 1: the end of the stable system prompt; conversation turns never change this entry.
                new() { Text = knowledge.Text, CacheControl = new CacheControlEphemeral() },
            },
            Messages = history,
            // Breakpoint 2 (automatic caching): moves to the last block as the conversation grows.
            CacheControl = new CacheControlEphemeral(),
        };

        if (!settings.CacheDiagnostics)
        {
            // Assigning null would send "diagnostics": null; leave the property unset instead.
            return request;
        }

        // previous_message_id is null on the first turn: opt in without anything to compare.
        return request with { Diagnostics = new DiagnosticsParam { PreviousMessageID = previousMessageId } };
    }

    public async Task<SupportAnswer> SendAsync(
        Guid conversationId, string customerText, string? operatorNote, CancellationToken cancellationToken)
    {
        Conversation conversation = conversations.Get(conversationId);
        List<MessageParam> history = [.. conversation.Messages, new UserMessageParam { Content = customerText }];

        if (operatorNote is not null)
        {
            // Goes right after the user turn; earlier turns stay byte-identical for the prompt cache.
            history.Add(new SystemMessageParam { Content = operatorNote });
        }

        MessageCreateParams request = BuildRequest(history, knowledge, options.Value, conversation.LastMessageId);
        Message message = await claude.Messages.Create(request, cancellationToken);
        ClaudeReply reply = ClaudeReplyReader.Read(message);

        decimal costUsd = usageMeter.Record("support", options.Value.Model, reply.Usage);
        logger.LogInformation(
            "Support {ConversationId}: outcome={Outcome} input={Input} cacheRead={CacheRead} cacheWrite={CacheWrite} output={Output} costUsd={CostUsd}",
            conversationId, reply.Outcome, reply.Usage.Input, reply.Usage.CacheRead, reply.Usage.CacheWrite, reply.Usage.Output, costUsd);
        LogCacheMiss(conversationId, message.Diagnostics);

        switch (reply.Outcome)
        {
            case ReplyOutcome.Complete:
            case ReplyOutcome.Truncated:
                history.Add(new AssistantMessageParam { Content = reply.AssistantContent.ToList() });
                conversations.Save(conversationId, new Conversation(history, message.ID));
                string text = reply.Outcome == ReplyOutcome.Truncated ? reply.Text + " [...]" : reply.Text;
                return new SupportAnswer(text, reply.Outcome, reply.Usage);

            case ReplyOutcome.Refused:
                // The refused turn is not saved; the customer gets a fixed answer and a person takes over.
                logger.LogWarning("Support {ConversationId} refused: category={Category}", conversationId, reply.RefusalCategory);
                return new SupportAnswer(RefusalText, reply.Outcome, reply.Usage);

            default:
                logger.LogError("Support {ConversationId}: unexpected outcome {Outcome}", conversationId, reply.Outcome);
                return new SupportAnswer(RefusalText, reply.Outcome, reply.Usage);
        }
    }

    // diagnostics is null when nothing diverged; cache_miss_reason is null while the comparison is still running.
    private void LogCacheMiss(Guid conversationId, Diagnostics? diagnostics)
    {
        if (diagnostics?.CacheMissReason is not { } reason)
        {
            return;
        }

        logger.LogWarning(
            "Support {ConversationId}: prompt cache miss reason={Reason} missedTokens={MissedTokens}",
            conversationId, reason.Type.Raw(), reason.CacheMissedInputTokens);
    }
}
