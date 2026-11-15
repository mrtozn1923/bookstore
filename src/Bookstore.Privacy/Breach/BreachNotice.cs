using System.Text;

namespace Bookstore.Privacy.Breach;

// Draft for the people affected; Kurul decision 2019/271 lists the minimum content. Legal fills the open lines.
public static class BreachNotice
{
    public static string Draft(BreachAssessment a, DateTime occurredFromUtc, DateTime occurredToUtc, string contact)
    {
        var text = new StringBuilder();
        text.AppendLine($"When: between {occurredFromUtc:yyyy-MM-dd HH:mm} and {occurredToUtc:yyyy-MM-dd HH:mm} UTC");
        text.AppendLine($"Personal data affected: {string.Join(", ", a.Categories)}");
        text.AppendLine($"Special category data affected: {(a.SpecialCategories.Length == 0 ? "none" : string.Join(", ", a.SpecialCategories))}");
        text.AppendLine("Possible consequences: [legal team]");
        text.AppendLine("Measures taken and recommended: [security and legal team]");
        text.AppendLine($"Contact: {contact}");
        return text.ToString();
    }
}
