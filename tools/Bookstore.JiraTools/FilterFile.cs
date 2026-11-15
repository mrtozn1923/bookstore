namespace Bookstore.JiraTools;

public static class FilterFile
{
    // Format: one filter per line, "name: JQL". Lines starting with # are comments.
    public static IEnumerable<(string Name, string Jql)> Read(string path) =>
        File.ReadLines(path)
            .Where(line => line.Trim() is { Length: > 0 } trimmed && !trimmed.StartsWith('#'))
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => (parts[0].Trim(), parts[1].Trim()));
}
