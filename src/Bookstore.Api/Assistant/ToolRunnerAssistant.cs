using System.Text.Json;
using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Beta.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Ordering;
using Microsoft.Extensions.Options;
using BetaInputSchema = Anthropic.Models.Beta.Messages.InputSchema;
using BetaMessageCreateParams = Anthropic.Models.Beta.Messages.MessageCreateParams;

namespace Bookstore.Api.Assistant;

// The same assistant with the SDK's tool runner (beta): the SDK runs the loop.
public sealed class ToolRunnerAssistant(IAnthropicClient claude, BookstoreTools tools, IOptions<ClaudeOptions> options)
{
    public async Task<string> AskAsync(string question, CurrentCustomer customer, CancellationToken cancellationToken)
    {
        BetaRunnableTool orderStatus = new()
        {
            Name = BookstoreTools.GetOrderStatus,
            Definition = new BetaTool
            {
                Name = BookstoreTools.GetOrderStatus,
                Description = "Returns the status of one order of the signed-in customer.",
                Strict = true,
                InputSchema = new BetaInputSchema(new Dictionary<string, JsonElement>
                {
                    ["type"] = JsonSerializer.SerializeToElement("object"),
                    ["properties"] = JsonSerializer.SerializeToElement(new { order_id = new { type = "string" } }),
                    ["required"] = JsonSerializer.SerializeToElement(new[] { "order_id" }),
                    ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
                }),
            },
            Run = (call, _) =>
            {
                try
                {
                    return Task.FromResult<BetaToolResultBlockParamContent>(tools.Run(call.Name, call.Input, customer));
                }
                catch (ToolInputException ex)
                {
                    // The runner turns BetaToolError into a tool_result with is_error: true.
                    throw new BetaToolError(ex.Message);
                }
            },
        };

        BetaToolRunner runner = claude.Beta.Messages.ToolRunner(
            new BetaMessageCreateParams
            {
                Model = options.Value.Model,
                MaxTokens = 4096,
                System = AssistantAgent.SystemPrompt,
                Messages = [new BetaUserMessageParam { Content = question }],
            },
            [orderStatus],
            maxIterations: AssistantAgent.MaxRounds);

        BetaMessage final = await runner.RunUntilDoneAsync(cancellationToken);
        return string.Concat(final.Content.Select(b => b.TryPickText(out BetaTextBlock? text) ? text.Text : string.Empty));
    }
}
