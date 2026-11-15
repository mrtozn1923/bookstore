namespace Bookstore.Prompts.Tasks.Localization;

public sealed record Glossary(Dictionary<string, string> Terms, List<string> DoNotTranslate);

public static class TranslationChecker
{
    public static List<string> Check(string source, string target, Glossary glossary)
    {
        List<string> issues = [];
        if (!target.TrimStart().StartsWith('<'))
        {
            issues.Add($"text before the HTML: \"{Shorten(target)}\"");
        }
        if (!TextFacts.Placeholders(source).SequenceEqual(TextFacts.Placeholders(target)))
        {
            issues.Add($"placeholders {string.Join(" ", TextFacts.Placeholders(source))} -> {string.Join(" ", TextFacts.Placeholders(target))}");
        }
        if (!TextFacts.Tags(source).SequenceEqual(TextFacts.Tags(target)))
        {
            issues.Add("HTML tags changed");
        }
        foreach ((string tr, string en) in glossary.Terms)
        {
            if (source.Contains(tr, StringComparison.OrdinalIgnoreCase) && !target.Contains(en, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"glossary: \"{tr}\" should be \"{en}\"");
            }
        }
        foreach (string term in glossary.DoNotTranslate)
        {
            if (source.Contains(term, StringComparison.Ordinal) && !target.Contains(term, StringComparison.Ordinal))
            {
                issues.Add($"\"{term}\" was changed");
            }
        }
        if (target.Any(c => "çğıöşüÇĞİÖŞÜ".Contains(c)))
        {
            issues.Add("Turkish text left in the output");
        }
        if (TextFacts.TurkishNumbers(target) is { Count: > 0 } turkishNumbers)
        {
            issues.Add($"Turkish number format: {string.Join(", ", turkishNumbers)}");
        }
        List<string> added = TextFacts.NewNumbers(source, target);
        if (added.Count > 0)
        {
            issues.Add($"new numbers: {string.Join(", ", added)}");
        }
        return issues;
    }

    static string Shorten(string text)
    {
        string oneLine = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return oneLine.Length <= 40 ? oneLine : oneLine[..37] + "...";
    }
}
