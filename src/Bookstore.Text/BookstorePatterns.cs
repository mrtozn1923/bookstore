using System.Text.RegularExpressions;

namespace Bookstore.Text;

public static partial class BookstorePatterns
{
    // \z, not $: a trailing \n must not pass
    [GeneratedRegex(@"^BKS-[0-9]{4}-[0-9]{6}\z")]
    private static partial Regex OrderNumber { get; }

    // letters, single separators between words; no nested quantifier, so no catastrophic backtracking
    [GeneratedRegex(@"^\p{L}+(?:[ '-]\p{L}+)*\z", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex CustomerName { get; }

    public static bool IsOrderNumber(string input) => OrderNumber.IsMatch(input);

    public static bool IsCustomerName(string input) => input.Length <= 100 && CustomerName.IsMatch(input);
}
