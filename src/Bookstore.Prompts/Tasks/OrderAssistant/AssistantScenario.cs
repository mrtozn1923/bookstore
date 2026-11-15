using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bookstore.Prompts.Tasks.OrderAssistant;

public sealed record Turn(string Role, string Text);

public sealed record ScenarioExpectation(
    List<string> Calls,
    List<string> NotCalls,
    List<string> TextMustNot,
    bool MustAsk = false);

public sealed record AssistantScenario(string Id, List<Turn> History, string Message, ScenarioExpectation Expected, string Note = "")
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<AssistantScenario> LoadJsonl(string path) =>
        [.. File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<AssistantScenario>(line, Json)!)];
}

// Grades the trajectory (which tools were called) and a few hard rules on the final text
public static class AssistantGrader
{
    public static List<string> Grade(ScenarioExpectation expected, IReadOnlyList<string> calls, string finalText)
    {
        List<string> issues = [];
        issues.AddRange(expected.Calls.Where(c => !calls.Contains(c)).Select(c => $"missing call {c}"));
        issues.AddRange(expected.NotCalls.Where(calls.Contains).Select(c => $"forbidden call {c}"));
        // Whole words only: "onaylandı" must not match "onaylandıktan sonra"
        issues.AddRange(expected.TextMustNot
            .Where(t => Regex.IsMatch(finalText, $@"\b{Regex.Escape(t)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Select(t => $"text contains \"{t}\""));
        if (expected.MustAsk && !AsksTheCustomer(finalText))
        {
            issues.Add("expected a question");
        }
        return issues;
    }

    // A question mark, or a polite request such as "Lütfen sipariş numaranızı belirtin."
    static bool AsksTheCustomer(string text) =>
        text.Contains('?') || Regex.IsMatch(text, @"\b(belirtin|belirtir misiniz|paylaşın|yazın|iletin)\b", RegexOptions.IgnoreCase);
}
