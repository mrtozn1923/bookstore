namespace Bookstore.Prompts.Tasks.CodePrompts;

public sealed record ReviewScore(int Findings, int Grounded, bool FoundPlantedBug);

// A finding counts only if the quoted line really is a changed line of the diff
public static class ReviewChecker
{
    public static IReadOnlyList<string> ChangedLines(string diff) =>
        [.. diff.Split('\n')
            .Where(l => (l.StartsWith('+') || l.StartsWith('-')) && !l.StartsWith("+++") && !l.StartsWith("---"))
            .Select(l => l[1..].Trim())
            .Where(l => l.Length > 0)];

    public static ReviewScore Score(ReviewResult review, string diff, string? plantedLine)
    {
        IReadOnlyList<string> changed = ChangedLines(diff);
        List<ReviewFinding> grounded =
            [.. review.Findings.Where(f => changed.Contains(f.Line.Trim().TrimStart('+', '-').Trim()))];
        bool found = plantedLine is not null && grounded.Any(f => f.Line.Contains(plantedLine));
        return new(review.Findings.Count, grounded.Count, found);
    }

    // Trace rows are checked against both versions of the method, so a wrong trace is visible too
    public static List<string> TraceErrors(IEnumerable<TraceRow> trace, Func<int, decimal> before, Func<int, decimal> after) =>
        [.. trace
            .Where(r => TestGenChecker.Normalize(r.Before) != TestGenChecker.Actual(before, r.Quantity)
                || TestGenChecker.Normalize(r.After) != TestGenChecker.Actual(after, r.Quantity))
            .Select(r => $"{r.Quantity}: model {r.Before}/{r.After}, code {TestGenChecker.Actual(before, r.Quantity)}/{TestGenChecker.Actual(after, r.Quantity)}")];
}
