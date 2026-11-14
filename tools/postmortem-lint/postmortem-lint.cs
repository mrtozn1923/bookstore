// Checks the action items of every postmortem in a folder and lists the overdue ones.
// Usage: dotnet run tools/postmortem-lint/postmortem-lint.cs -- [folder=docs/postmortems] [today=yyyy-MM-dd]
// Exit code 1 if an action item is incomplete, so it can run as a CI step.
using System.Globalization;
using System.Text.RegularExpressions;

string folder = args.Length > 0 ? args[0] : "docs/postmortems";
DateOnly today = args.Length > 1
    ? DateOnly.ParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture)
    : DateOnly.FromDateTime(DateTime.UtcNow);
string[] classes = ["prevent", "detect", "mitigate", "process"];
string[] notAnOwner = ["", "team", "everyone", "tbd", "?"];

int problems = 0, open = 0, overdue = 0, total = 0;
foreach (string file in Directory.GetFiles(folder, "*.md").Where(f => Path.GetFileName(f) != "README.md").Order())
{
    List<string[]> rows = ActionItemRows(File.ReadAllLines(file));
    if (rows.Count == 0)
    {
        Report(file, "-", "no action items (a postmortem without one needs a sentence saying why)");
        continue;
    }
    foreach (string[] c in rows)
    {
        // | ID | Action | Class | Owner | Due | Ticket | Status |
        total++;
        string id = c[0];
        if (!classes.Contains(c[2].ToLowerInvariant()))
        {
            Report(file, id, $"class '{c[2]}' is not one of {string.Join("/", classes)}");
        }
        if (notAnOwner.Contains(c[3].ToLowerInvariant()))
        {
            Report(file, id, "needs one named owner");
        }
        if (!Regex.IsMatch(c[5], @"^[A-Z]+-\d+$"))
        {
            Report(file, id, $"needs a ticket (got '{c[5]}')");
        }
        bool isOpen = !c[6].Equals("done", StringComparison.OrdinalIgnoreCase);
        if (!DateOnly.TryParseExact(c[4], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly due))
        {
            Report(file, id, $"due date '{c[4]}' is not yyyy-MM-dd");
        }
        else if (isOpen && due < today)
        {
            overdue++;
            Console.WriteLine($"OVERDUE {Path.GetFileName(file)} {id}: {c[1]} (owner {c[3]}, due {due:yyyy-MM-dd}, {today.DayNumber - due.DayNumber} days late)");
        }
        if (isOpen)
        {
            open++;
        }
    }
}

Console.WriteLine($"{total} action items, {open} open, {overdue} overdue, {problems} problems");
return problems == 0 ? 0 : 1;

void Report(string file, string id, string message)
{
    problems++;
    Console.WriteLine($"ERROR {Path.GetFileName(file)} {id}: {message}");
}

// Rows of the first Markdown table under "## Action items", without the header and separator lines.
static List<string[]> ActionItemRows(string[] lines)
{
    List<string[]> rows = [];
    int start = Array.FindIndex(lines, l => l.Trim().Equals("## Action items", StringComparison.OrdinalIgnoreCase));
    if (start < 0)
    {
        return rows;
    }
    foreach (string line in lines.Skip(start + 1).SkipWhile(l => !l.TrimStart().StartsWith('|')))
    {
        if (!line.TrimStart().StartsWith('|'))
        {
            break;
        }
        string[] cells = [.. line.Trim().Trim('|').Split('|').Select(c => c.Trim())];
        if (cells.Length >= 7 && cells[0] != "ID" && !cells[0].StartsWith("---"))
        {
            rows.Add(cells);
        }
    }
    return rows;
}
