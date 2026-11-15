using Bookstore.Prompts.Catalog;
using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.ReturnCheck;

// The text lives in prompts/return-check/*.prompt.md; the output format stays in code next to its type
public static class ReturnCheckPrompts
{
    public static PromptVersion V1 { get; } =
        PromptCatalog.Default.ToPromptVersion("return-check", "v1", "message", ChatResponseFormat.Json);

    public static PromptVersion V2 { get; } =
        PromptCatalog.Default.ToPromptVersion("return-check", "v2", "message", ChatResponseFormat.ForJsonSchema<ReturnCheckResult>());

    public static PromptVersion V3 { get; } =
        PromptCatalog.Default.ToPromptVersion("return-check", "v3", "message", ChatResponseFormat.ForJsonSchema<ReturnCheckResult>());

    public static PromptVersion Get(string version) =>
        PromptCatalog.Default.ToPromptVersion("return-check", version, "message", version == "v1"
            ? ChatResponseFormat.Json
            : ChatResponseFormat.ForJsonSchema<ReturnCheckResult>());
}
