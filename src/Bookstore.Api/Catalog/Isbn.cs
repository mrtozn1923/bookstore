using System.Text.RegularExpressions;

namespace Bookstore.Catalog;

public static class Isbn
{
    private static readonly Regex Isbn13Pattern = new(
        @"^(?:ISBN[- ]?)?(97[89])[- ]?(\d{1,5})[- ]?(\d+)[- ]?(\d+)[- ]?(\d)$", RegexOptions.Compiled);

    // Normalizes "ISBN 978-0-441-17271-9" to "9780441172719"; returns null for anything else.
    public static string? Normalize(string input)
    {
        string candidate = input.Trim();
        if (Isbn13Pattern.IsMatch(candidate))
        {
            Match match = Isbn13Pattern.Match(candidate);
            string digits = string.Concat(match.Groups.Values.Skip(1).Select(g => g.Value));
            return digits.Length == 13 ? digits : null;
        }

        return null;
    }
}
