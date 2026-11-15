using System.Text.RegularExpressions;

namespace Bookstore.JiraTools;

public sealed record SmartCommitLine(int Number, IReadOnlyList<string> Keys, IReadOnlyList<string> Commands,
    IReadOnlyList<string> Problems);

public static partial class SmartCommit
{
    [GeneratedRegex(@"\b[A-Z]{2,}-\d+\b")]
    private static partial Regex Key();

    [GeneratedRegex(@"\b[a-z]{2,}-\d+\b")]
    private static partial Regex LowercaseKey();

    [GeneratedRegex(@"#(?<name>[A-Za-z][\w-]*)")]
    private static partial Regex Command();

    public static IReadOnlyList<SmartCommitLine> Parse(string message, IReadOnlyList<string> transitions)
    {
        List<SmartCommitLine> result = [];
        string[] lines = message.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            List<string> keys = [.. Key().Matches(line).Select(m => m.Value)];
            List<string> commands = [.. Command().Matches(line).Select(m => m.Groups["name"].Value)];
            List<string> problems = [];

            if (keys.Count == 0 && LowercaseKey().Match(line) is { Success: true } lower)
                problems.Add($"{lower.Value} is lowercase; Jira will not link it");
            if (keys.Count == 0 && commands.Count > 0)
                problems.Add("commands ignored: no work item key on this line");
            foreach (string command in commands.Where(c => c is not ("comment" or "time")))
                problems.AddRange(CheckTransition(command, transitions));

            if (keys.Count > 0 || commands.Count > 0 || problems.Count > 0)
                result.Add(new SmartCommitLine(i + 1, keys, commands, problems));
        }
        return result;
    }

    // Jira matches the part of a transition name before the first space;
    // a hyphenated full name ("request-review") picks one when that is ambiguous.
    private static IEnumerable<string> CheckTransition(string command, IReadOnlyList<string> transitions)
    {
        string[] exact = [.. transitions.Where(t => t.Replace(' ', '-').Equals(command, StringComparison.OrdinalIgnoreCase))];
        if (exact.Length == 1)
            yield break;
        string[] byFirstWord = [.. transitions.Where(t => t.Split(' ')[0].Equals(command, StringComparison.OrdinalIgnoreCase))];
        if (byFirstWord.Length == 0)
            yield return $"#{command} matches no transition";
        else if (byFirstWord.Length > 1)
            yield return $"#{command} is ambiguous: {string.Join(", ", byFirstWord)}";
    }
}
