using System.Reflection;

namespace Bookstore.Api.Support;

// The long, stable part of the support prompt. It is built once and sent byte-for-byte the same on every request.
public sealed class SupportKnowledge(string policy, IEnumerable<string> categoryPaths)
{
    public string Text { get; } = policy.TrimEnd() + "\n\n## Category tree\n\n" + RenderCategoryTree(categoryPaths);

    // The same paths in any order give the same text; a cache hit needs a byte-identical prefix.
    public static string RenderCategoryTree(IEnumerable<string> categoryPaths) =>
        string.Join('\n', categoryPaths
            .Select(path => path.Trim())
            .Where(path => path.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(path => "- " + path));

    public static SupportKnowledge LoadEmbedded()
    {
        string policy = ReadResource("policy.md");
        string[] categories = ReadResource("categories.txt").Split('\n');
        return new SupportKnowledge(policy, categories);
    }

    private static string ReadResource(string fileName)
    {
        Assembly assembly = typeof(SupportKnowledge).Assembly;
        string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".Knowledge." + fileName, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using StreamReader reader = new(stream);
        // Normalize line endings so a Windows checkout and a Linux build send the same bytes.
        return reader.ReadToEnd().ReplaceLineEndings("\n");
    }
}
