using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Returns;

public sealed record ExtractionResult(ReturnRequestDraft? Draft, ReplyOutcome Outcome, string? Problem, TokenUsage Usage);

public sealed class ReturnRequestExtractor(IAnthropicClient claude, IOptions<ClaudeOptions> options)
{
    private const string SystemPrompt = """
        You turn a customer's free-text return request for Bookstore, an online bookstore, into the given JSON structure.
        Use only what the customer wrote. If the order number is not in the text, use null; never invent one.
        Urgency is high only if the customer mentions a deadline or a damaged gift.
        """;

    // Built once: the API compiles the schema into a grammar and caches it for 24 hours.
    private static readonly JsonOutputFormat Format = new() { Schema = ClaudeJsonSchema.AsSchemaDictionary<ReturnRequestDraft>() };

    public MessageCreateParams BuildRequest(string customerText) => new()
    {
        Model = options.Value.Model,
        MaxTokens = 2048,
        System = SystemPrompt,
        OutputConfig = new OutputConfig { Format = Format, Effort = Effort.Low },
        Messages = [new UserMessageParam { Content = customerText }],
    };

    public async Task<ExtractionResult> ExtractAsync(string customerText, CancellationToken cancellationToken)
    {
        Message message = await claude.Messages.Create(BuildRequest(customerText), cancellationToken);
        ClaudeReply reply = ClaudeReplyReader.Read(message);

        // Refusal and max_tokens can leave text that does not match the schema; do not parse it.
        if (reply.Outcome != ReplyOutcome.Complete)
        {
            return new ExtractionResult(null, reply.Outcome, $"No structured output ({reply.Outcome}).", reply.Usage);
        }

        try
        {
            ReturnRequestDraft? draft = JsonSerializer.Deserialize<ReturnRequestDraft>(reply.Text, ClaudeJsonSchema.SerializerOptions);
            return new ExtractionResult(draft, reply.Outcome, draft is null ? "Empty output." : null, reply.Usage);
        }
        catch (JsonException ex)
        {
            return new ExtractionResult(null, reply.Outcome, $"Output did not deserialize: {ex.Message}", reply.Usage);
        }
    }
}
