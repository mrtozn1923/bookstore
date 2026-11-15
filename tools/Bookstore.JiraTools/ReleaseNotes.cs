using System.Text;

namespace Bookstore.JiraTools;

public sealed record WorkItem(string Key, string Type, string Summary);

public static class ReleaseNotes
{
    private static readonly (string Type, string Heading)[] Sections =
        [("Story", "New"), ("Bug", "Fixed"), ("Task", "Other changes")];

    public static string Render(string title, IReadOnlyList<WorkItem> items)
    {
        StringBuilder text = new();
        text.AppendLine($"# {title}");
        foreach ((string type, string heading) in Sections)
        {
            WorkItem[] section = [.. items.Where(i => i.Type == type).OrderBy(i => i.Key, StringComparer.Ordinal)];
            if (section.Length == 0)
                continue;
            text.AppendLine().AppendLine($"## {heading}");
            foreach (WorkItem item in section)
                text.AppendLine($"- {item.Summary} ({item.Key})");
        }
        int skipped = items.Count(i => !Sections.Any(s => s.Type == i.Type));
        if (skipped > 0)
            text.AppendLine().AppendLine($"Left out (other work types): {skipped}");
        return text.ToString();
    }

    public static readonly IReadOnlyList<WorkItem> SampleItems =
    [
        new("BOOK-142", "Story", "Institutions get 10% off orders of 20 or more copies"),
        new("BOOK-147", "Story", "Approvers receive an e-mail when a purchase needs approval"),
        new("BOOK-151", "Bug", "VAT rounding on the invoice PDF is off by 0.01 TRY"),
        new("BOOK-153", "Bug", "Cart total drops the institutional discount after login"),
        new("BOOK-155", "Task", "Index Orders by InstitutionId"),
        new("BOOK-120", "Epic", "Institutional ordering, release 1"),
    ];
}
