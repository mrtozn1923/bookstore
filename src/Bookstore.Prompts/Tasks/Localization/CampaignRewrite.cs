using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Tasks.Localization;

// Facts the short text must keep, written by the marketing team next to the long text
public sealed record Campaign(string Text, List<string> MustKeep);

public static class CampaignRewrite
{
    public const int MaxLength = 120;

    public static readonly PromptVersion V1 = new(
        Task: "campaign-push",
        Version: "v1",
        System: $"Shorten the campaign text to under {MaxLength} characters for a push notification. Keep it in Turkish.",
        BuildUser: text => text);

    public static readonly PromptVersion V2 = new(
        Task: "campaign-push",
        Version: "v2",
        System: $"""
            You write Turkish push notifications for Bookstore campaigns.

            ## Rules
            - One sentence, 80 to 110 characters including spaces. Never more than {MaxLength}.
            - Keep the discount, the campaign code and the last day exactly as written.
            - No emoji, no exclamation marks, no information that is not in the campaign text.
            - Output only the notification text.
            """,
        BuildUser: text => $"<campaign>\n{text}\n</campaign>");

    public static List<string> Check(Campaign campaign, string output)
    {
        string text = output.Trim();
        List<string> issues = [];
        if (text.Length > MaxLength)
        {
            issues.Add($"{text.Length} characters, limit is {MaxLength}");
        }
        if (text.Contains('!'))
        {
            issues.Add("exclamation mark");
        }
        if (text.Any(char.IsSurrogate))
        {
            issues.Add("emoji");
        }
        foreach (string fact in campaign.MustKeep.Where(f => !text.Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add($"missing \"{fact}\"");
        }
        List<string> added = TextFacts.NewNumbers(campaign.Text, text);
        if (added.Count > 0)
        {
            issues.Add($"new numbers: {string.Join(", ", added)}");
        }
        return issues;
    }
}
