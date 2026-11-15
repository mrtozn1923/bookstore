namespace Bookstore.Prompts.Tasks.LongDocument;

public sealed record ContractSection(string Heading, string Text);

// The contract as one text and as sections; sections are split at "## " headings
public sealed class ContractDocument(string source, string text)
{
    public string Source { get; } = source;
    public string Text { get; } = text;

    public IReadOnlyList<ContractSection> Sections { get; } = Split(text);

    public static ContractDocument Load(string path) => new(Path.GetFileName(path), File.ReadAllText(path));

    // Consecutive sections grouped into chunks for map-reduce
    public IEnumerable<string> Chunks(int sectionsPerChunk) =>
        Sections.Chunk(sectionsPerChunk).Select(c => string.Join("\n\n", c.Select(s => s.Text)));

    public bool Contains(string quote) => Squash(Text).Contains(Squash(quote), StringComparison.Ordinal);

    static string Squash(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    static List<ContractSection> Split(string text)
    {
        List<ContractSection> sections = [];
        string[] parts = text.Split("\n## ");
        foreach (string part in parts.Skip(1))
        {
            string heading = part[..part.IndexOf('\n')].Trim();
            sections.Add(new ContractSection(heading, "## " + part.Trim()));
        }
        return sections;
    }
}
