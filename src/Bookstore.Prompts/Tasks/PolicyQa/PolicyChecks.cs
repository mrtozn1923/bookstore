using System.Text.RegularExpressions;

namespace Bookstore.Prompts.Tasks.PolicyQa;

public sealed record PolicyExpectation(string Reference, List<string> Facts, string Label = "");

// Deterministic checks: cheap, run on every pull request, no model needed
public static partial class PolicyChecks
{
    public const int MaxLength = 400;

    public static List<string> Check(string question, string answer, PolicyExpectation expected, string policy)
    {
        List<string> issues = [];
        foreach (string fact in expected.Facts)
        {
            // "a|b" means either form counts
            if (!fact.Split('|').Any(f => answer.Contains(f, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add($"missing fact: {fact}");
            }
        }
        if (answer.Length > MaxLength)
        {
            issues.Add($"too long: {answer.Length} chars");
        }
        HashSet<string> known = [.. Numbers(policy), .. Numbers(question), .. Numbers(expected.Reference)];
        List<string> invented = [.. Numbers(answer).Where(n => !known.Contains(n)).Distinct()];
        if (invented.Count > 0)
        {
            issues.Add($"numbers not in the policy: {string.Join(", ", invented)}");
        }
        return issues;
    }

    static IEnumerable<string> Numbers(string text) => Number().Matches(text).Select(m => m.Value);

    [GeneratedRegex(@"\d+(?:,\d+)?")]
    private static partial Regex Number();
}
