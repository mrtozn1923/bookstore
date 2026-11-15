using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;

namespace Bookstore.Api.Returns;

public sealed record ClassifierSettings(string Model, Effort Effort, ThinkingConfigParam? Thinking = null);

public sealed record Classification(ReturnReason? Reason, string RawText, ReplyOutcome Outcome, TokenUsage Usage);

public sealed class ReturnClassifier(IAnthropicClient claude)
{
    public const string SystemPrompt = """
        You classify return requests for Bookstore, an online bookstore.
        Reply with exactly one label and nothing else:
        Damaged, WrongItem, NotAsDescribed, ChangedMind, LateDelivery, Other.
        Damaged: the book arrived torn, wet or broken. WrongItem: a different title or edition arrived.
        NotAsDescribed: the book differs from the product page. ChangedMind: the customer no longer wants it.
        LateDelivery: it arrived too late to be useful. Other: anything else.
        """;

    public static MessageCreateParams BuildRequest(string requestText, ClassifierSettings settings)
    {
        MessageCreateParams request = new()
        {
            Model = settings.Model,
            // Small answer, but leave room for thinking at higher effort.
            MaxTokens = 2048,
            System = SystemPrompt,
            OutputConfig = new OutputConfig { Effort = settings.Effort },
            Messages = [new UserMessageParam { Content = requestText }],
        };

        // Leave thinking out unless asked: each model has its own default.
        return settings.Thinking is null ? request : request with { Thinking = settings.Thinking };
    }

    public async Task<Classification> ClassifyAsync(string requestText, ClassifierSettings settings, CancellationToken cancellationToken)
    {
        Message message = await claude.Messages.Create(BuildRequest(requestText, settings), cancellationToken);
        ClaudeReply reply = ClaudeReplyReader.Read(message);
        return new Classification(ParseLabel(reply.Text), reply.Text, reply.Outcome, reply.Usage);
    }

    public static ReturnReason? ParseLabel(string text) =>
        Enum.TryParse(text.Trim().TrimEnd('.'), ignoreCase: true, out ReturnReason reason) ? reason : null;
}
