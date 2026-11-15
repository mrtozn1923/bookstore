using System.Text.Json;
using Bookstore.Prompts.Catalog;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.PolicyQa;

public sealed record JudgeVerdict(string Reasoning, string Verdict);

public sealed record PairwiseVerdict(string Reasoning, string Winner);

// LLM-as-judge: the rubric is a versioned prompt like any other, and the judge model differs from the generator
public sealed class PolicyJudge(IChatClient judge, ChatOptions baseOptions, PromptCatalog catalog)
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // A small judge can reason forever; a cut-off answer counts as "no verdict", never as a pass
    public const int MaxOutputTokens = 4096;

    public async Task<JudgeVerdict> GradeAsync(string question, string reference, string answer, CancellationToken cancellationToken = default)
    {
        PromptTemplate template = catalog.Get("policy-judge", "v1");
        ChatOptions options = baseOptions.Clone();
        options.ResponseFormat = ChatResponseFormat.ForJsonSchema<JudgeVerdict>();
        options.MaxOutputTokens = MaxOutputTokens;
        ChatResponse response = await judge.GetResponseAsync(
            template.Render(new Dictionary<string, string> { ["question"] = question, ["reference"] = reference, ["answer"] = answer }),
            options, cancellationToken);
        return Parse<JudgeVerdict>(response.Text) ?? new JudgeVerdict(response.Text, "no verdict");
    }

    public async Task<PairwiseVerdict> CompareAsync(string question, string reference, string a, string b, CancellationToken cancellationToken = default)
    {
        PromptTemplate template = catalog.Get("policy-pairwise", "v1");
        ChatOptions options = baseOptions.Clone();
        options.ResponseFormat = ChatResponseFormat.ForJsonSchema<PairwiseVerdict>();
        options.MaxOutputTokens = MaxOutputTokens;
        ChatResponse response = await judge.GetResponseAsync(
            template.Render(new Dictionary<string, string> { ["question"] = question, ["reference"] = reference, ["a"] = a, ["b"] = b }),
            options, cancellationToken);
        return Parse<PairwiseVerdict>(response.Text) ?? new PairwiseVerdict(response.Text, "none");
    }

    static T? Parse<T>(string text) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
