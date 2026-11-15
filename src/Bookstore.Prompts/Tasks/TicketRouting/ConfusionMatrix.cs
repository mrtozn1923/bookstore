using System.Text;

namespace Bookstore.Prompts.Tasks.TicketRouting;

public sealed class ConfusionMatrix(IReadOnlyList<string> labels)
{
    readonly Dictionary<(string Expected, string Actual), int> counts = [];

    public int Total { get; private set; }
    public int Correct { get; private set; }

    public void Add(string expected, string actual)
    {
        counts[(expected, actual)] = counts.GetValueOrDefault((expected, actual)) + 1;
        Total++;
        Correct += expected == actual ? 1 : 0;
    }

    // Rows: expected label, columns: predicted label; "?" collects labels outside the set
    public string Render()
    {
        List<string> columns = [.. labels, "?"];
        StringBuilder text = new();
        text.Append($"{"expected \\ got",-15}");
        foreach (string column in columns)
        {
            text.Append($"{Short(column),6}");
        }
        text.AppendLine();
        foreach (string row in labels)
        {
            text.Append($"{row,-15}");
            foreach (string column in columns)
            {
                int n = column == "?"
                    ? counts.Where(c => c.Key.Expected == row && !labels.Contains(c.Key.Actual)).Sum(c => c.Value)
                    : counts.GetValueOrDefault((row, column));
                text.Append($"{(n == 0 ? "." : n.ToString()),6}");
            }
            text.AppendLine();
        }
        text.Append($"accuracy {Correct}/{Total}");
        return text.ToString();
    }

    static string Short(string label) => label.Length <= 5 ? label : label[..5];
}
