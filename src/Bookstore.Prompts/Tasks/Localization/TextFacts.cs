using System.Text.RegularExpressions;

namespace Bookstore.Prompts.Tasks.Localization;

public static partial class TextFacts
{
    // "1.240,50" and "1,240.50" are the same number: compare digits only
    public static HashSet<string> Numbers(string text) =>
        [.. NumberPattern().Matches(text).Select(m => new string([.. m.Value.Where(char.IsDigit)]))];

    public static List<string> Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(m => m.Value).Order()];

    public static List<string> Tags(string text) =>
        [.. TagPattern().Matches(text).Select(m => m.Value)];

    // Numbers in the output that the source never mentioned
    public static List<string> NewNumbers(string source, string output) =>
        [.. Numbers(output).Except(Numbers(source)).Order()];

    // 1.240,50 or 989,90: a decimal comma, which an English page must not show
    public static List<string> TurkishNumbers(string text) =>
        [.. TurkishNumberPattern().Matches(text).Select(m => m.Value)];

    [GeneratedRegex(@"\b\d{1,3}(?:\.\d{3})*,\d{2}\b")]
    private static partial Regex TurkishNumberPattern();

    [GeneratedRegex(@"\d[\d.,]*\d|\d")]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"\{[A-Za-z]+\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"</?[a-z]+[^>]*>")]
    private static partial Regex TagPattern();
}
