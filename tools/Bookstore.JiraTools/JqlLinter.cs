using System.Text.RegularExpressions;

namespace Bookstore.JiraTools;

public enum Severity { Info, Warn }

public sealed record LintRule(string Id, Severity Severity, Regex Pattern, string Message, bool WhenMissing = false);

public sealed record Finding(string RuleId, Severity Severity, string Message);

public static class JqlLinter
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public static readonly IReadOnlyList<LintRule> Rules =
    [
        new("JQL001", Severity.Warn, new(@"""epic link""", Options),
            "\"Epic Link\" was retired in February 2024; use parent = KEY"),
        new("JQL002", Severity.Warn, new(@"!?=\s*(openSprints|closedSprints|futureSprints)\(\)", Options),
            "sprint functions only support IN / NOT IN"),
        new("JQL003", Severity.Warn, new(@"!?=\s*membersOf\(", Options),
            "membersOf() only supports IN / NOT IN"),
        new("JQL004", Severity.Warn, new(@"[<>=]\s*(?<value>-\d+[wdhm])\b", Options),
            "relative date {0} without quotes; write \"{0}\""),
        new("JQL005", Severity.Info, new(@"\bstatus\s*(=|in)\s*\(?\s*""?done""?", Options),
            "status name Done is workflow-specific; statusCategory = Done covers every finished status"),
        new("JQL006", Severity.Info, new(@"currentUser\(\)", Options),
            "currentUser() needs a logged-in user; do not share this filter anonymously"),
        new("JQL007", Severity.Info, new(@"\border\s+by\b", Options),
            "no ORDER BY; say which order the readers of this filter expect", WhenMissing: true),
    ];

    public static IReadOnlyList<Finding> Check(string jql)
    {
        List<Finding> findings = [];
        foreach (LintRule rule in Rules)
        {
            Match match = rule.Pattern.Match(jql);
            // WhenMissing rules report when there is NO match (ORDER BY, for example)
            if (match.Success == rule.WhenMissing)
                continue;
            string value = match.Groups["value"].Value;
            findings.Add(new Finding(rule.Id, rule.Severity, string.Format(rule.Message, value)));
        }
        return findings;
    }
}
