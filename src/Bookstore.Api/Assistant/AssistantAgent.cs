using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Ordering;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Assistant;

public sealed record AgentAnswer(string Text, ReplyOutcome Outcome, int Rounds, IReadOnlyList<string> ToolCalls, TokenUsage Usage);

public sealed class AssistantAgent(
    IAnthropicClient claude,
    BookstoreTools tools,
    IOptions<ClaudeOptions> options,
    ILogger<AssistantAgent> logger)
{
    public const int MaxRounds = 5;

    public const string SystemPrompt = """
        You are the shopping assistant of Bookstore, an online bookstore. Answer in the customer's language.
        Use search_books for book questions and get_order_status for order questions; never guess prices or order states.
        Use web search only for questions about an author's life, and cite the source.
        If a tool returns an error, explain it briefly and ask the customer for what is missing.
        """;

    public MessageCreateParams BuildRequest(IReadOnlyList<MessageParam> messages) => new()
    {
        Model = options.Value.Model,
        MaxTokens = 4096,
        System = SystemPrompt,
        Tools = [.. BookstoreTools.Definitions, BookstoreTools.AuthorWebSearchTool],
        Messages = messages,
    };

    public async Task<AgentAnswer> AskAsync(string question, CurrentCustomer customer, CancellationToken cancellationToken)
    {
        List<MessageParam> messages = [new UserMessageParam { Content = question }];
        List<string> toolCalls = [];
        long input = 0, output = 0;

        for (int round = 1; round <= MaxRounds; round++)
        {
            Message response = await claude.Messages.Create(BuildRequest(messages), cancellationToken);
            ClaudeReply reply = ClaudeReplyReader.Read(response);
            input += reply.Usage.Input;
            output += reply.Usage.Output;
            TokenUsage usage = new(input, output, 0, 0, 0);

            if (reply.Outcome is not (ReplyOutcome.NeedsTools or ReplyOutcome.Paused))
            {
                return new AgentAnswer(reply.Text, reply.Outcome, round, toolCalls, usage);
            }

            // The assistant turn goes back unchanged, including thinking and tool_use blocks.
            messages.Add(new AssistantMessageParam { Content = reply.AssistantContent.ToList() });

            if (reply.Outcome == ReplyOutcome.Paused)
            {
                // pause_turn: a server tool loop hit its limit; sending the turn back lets Claude continue.
                continue;
            }

            List<ContentBlockParam> results = [];
            foreach (ContentBlock block in response.Content)
            {
                if (block.TryPickToolUse(out ToolUseBlock? call))
                {
                    toolCalls.Add(call.Name);
                    results.Add(tools.Execute(call, customer));
                }
            }

            // Every tool_result goes back in ONE user message, and nothing comes before them.
            messages.Add(new UserMessageParam { Content = results });
            logger.LogInformation("Round {Round}: ran {Count} tool call(s)", round, results.Count);
        }

        logger.LogWarning("Stopped after {MaxRounds} rounds", MaxRounds);
        return new AgentAnswer("I could not finish this request. A team member will help you.", ReplyOutcome.Unknown,
            MaxRounds, toolCalls, new TokenUsage(input, output, 0, 0, 0));
    }
}
