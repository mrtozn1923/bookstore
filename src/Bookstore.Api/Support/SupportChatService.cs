using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Support;

public sealed record SupportAnswer(string Text, ReplyOutcome Outcome, TokenUsage Usage);

public sealed class SupportChatService(
    IAnthropicClient claude,
    IConversationStore conversations,
    IOptions<ClaudeOptions> options,
    ILogger<SupportChatService> logger)
{
    public const string SystemPrompt = """
        You are the customer support assistant of Bookstore, an online bookstore.
        Answer in the customer's language. Keep answers under 120 words.
        You can explain orders, shipping, returns and payments in general terms.
        You cannot see order data yet; if the customer needs a specific order checked, say that a team member will follow up.
        Returns are accepted within 14 days of delivery for unused books.
        """;

    // Room for adaptive thinking plus a 120-word answer; thinking counts toward max_tokens.
    public const int MaxTokens = 2048;

    private const string RefusalText = "I can't help with this request here. A member of our support team will contact you.";

    public static MessageCreateParams BuildRequest(IReadOnlyList<MessageParam> history, ClaudeOptions settings) => new()
    {
        Model = settings.Model,
        MaxTokens = MaxTokens,
        System = SystemPrompt,
        Messages = history,
    };

    public async Task<SupportAnswer> SendAsync(
        Guid conversationId, string customerText, string? operatorNote, CancellationToken cancellationToken)
    {
        List<MessageParam> history = [.. conversations.Get(conversationId), new UserMessageParam { Content = customerText }];

        if (operatorNote is not null)
        {
            // Goes right after the user turn; earlier turns stay byte-identical for the prompt cache.
            history.Add(new SystemMessageParam { Content = operatorNote });
        }

        Message message = await claude.Messages.Create(BuildRequest(history, options.Value), cancellationToken);
        ClaudeReply reply = ClaudeReplyReader.Read(message);

        logger.LogInformation(
            "Support {ConversationId}: outcome={Outcome} input={Input} output={Output} thinking={Thinking}",
            conversationId, reply.Outcome, reply.Usage.Input, reply.Usage.Output, reply.Usage.Thinking);

        switch (reply.Outcome)
        {
            case ReplyOutcome.Complete:
            case ReplyOutcome.Truncated:
                history.Add(new AssistantMessageParam { Content = reply.AssistantContent.ToList() });
                conversations.Save(conversationId, history);
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
}
