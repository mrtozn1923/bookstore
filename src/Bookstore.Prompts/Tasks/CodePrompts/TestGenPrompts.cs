using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.CodePrompts;

public sealed record GeneratedCase(int Quantity, string Expected, string? Partition);

public sealed record GeneratedTests(List<string>? Partitions, List<GeneratedCase> Cases);

// v1 asks only for cases; partitions are not part of its output
public sealed record GeneratedCaseList(List<GeneratedCase> Cases);

public static class TestGenPrompts
{
    public const string Spec = """
        decimal InstitutionalDiscount.RateFor(int quantity)
        Institutional orders of 1 to 19 copies get no discount (0).
        Orders of 20 to 5000 copies get a 10% discount (0.10).
        A quantity below 1 or above 5000 throws ArgumentOutOfRangeException; larger orders need a quote.
        """;

    // v1: "write tests", output shape only
    public static readonly PromptVersion V1 = new(
        Task: "test-gen",
        Version: "v1",
        System: "You write unit test cases for C# methods. Expected is \"0\", \"0.10\" or \"throws\".",
        BuildUser: spec => $"Write test cases for this method.\n\n{spec}",
        Format: ChatResponseFormat.ForJsonSchema<GeneratedCaseList>());

    // v2: partitions first, then both sides of every boundary; the expected value comes from the rule
    public static readonly PromptVersion V2 = new(
        Task: "test-gen",
        Version: "v2",
        System: """
            You design unit test cases from a specification, not from an implementation.

            1. List the equivalence partitions of the input, valid and invalid, in partitions.
            2. For every boundary between two partitions, add one case on each side of it.
            3. Add one typical value from the middle of every partition.
            4. Expected is "0", "0.10" or "throws", derived only from the specification.
               If the specification does not decide a case, leave that case out.
            Return only a JSON object that matches the schema.
            """,
        BuildUser: spec => $"<specification>\n{spec}\n</specification>",
        Format: ChatResponseFormat.ForJsonSchema<GeneratedTests>());
}
