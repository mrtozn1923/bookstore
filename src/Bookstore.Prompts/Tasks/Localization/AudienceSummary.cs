using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Tasks.Localization;

// Same incident, two readers: the summary is defined by who reads it
public static class AudienceSummary
{
    public static readonly PromptVersion Management = new(
        Task: "incident-summary",
        Version: "management",
        System: """
            Summarize the Bookstore incident report for the management team, in Turkish.
            Exactly 3 bullet lines starting with "- ": impact with numbers, cause, action taken.
            Use only numbers that appear in the report. Output only the bullets.
            """,
        BuildUser: report => $"<report>\n{report}\n</report>");

    public static readonly PromptVersion Customer = new(
        Task: "incident-summary",
        Version: "customer",
        System: """
            Write a short Turkish notification to an affected Bookstore customer based on the
            incident report. At most 2 sentences and 300 characters. Say what happened to their
            order and what Bookstore offers. Do not mention internal details: order counts,
            costs, system or team names, root cause. Output only the notification.
            """,
        BuildUser: report => $"<report>\n{report}\n</report>");

    public static List<string> CheckManagement(string report, string output)
    {
        List<string> issues = [];
        string[] lines = output.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != 3 || !lines.All(l => l.TrimStart().StartsWith("- ")))
        {
            issues.Add($"expected 3 bullet lines, got {lines.Length} lines");
        }
        List<string> added = TextFacts.NewNumbers(report, output);
        if (added.Count > 0)
        {
            issues.Add($"new numbers: {string.Join(", ", added)}");
        }
        return issues;
    }

    public static List<string> CheckCustomer(string report, string output, IEnumerable<string> internalTerms)
    {
        string text = output.Trim();
        List<string> issues = [];
        if (text.Length > 300)
        {
            issues.Add($"{text.Length} characters, limit is 300");
        }
        issues.AddRange(internalTerms
            .Where(t => text.Contains(t, StringComparison.OrdinalIgnoreCase))
            .Select(t => $"internal detail \"{t}\""));
        List<string> added = TextFacts.NewNumbers(report, text);
        if (added.Count > 0)
        {
            issues.Add($"new numbers: {string.Join(", ", added)}");
        }
        return issues;
    }
}
