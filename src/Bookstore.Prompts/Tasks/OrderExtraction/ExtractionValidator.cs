namespace Bookstore.Prompts.Tasks.OrderExtraction;

// Checks the schema cannot express; any issue sends the email to a person
public static class ExtractionValidator
{
    public static List<string> Validate(Extraction extraction, OrderEmail email)
    {
        List<string> issues = [];
        if (extraction.Items.Count == 0)
        {
            issues.Add("no items found");
        }
        // Position of every digit in the body, so "978-0-14-..." can be found by its digits
        List<int> digitPositions = [.. email.Body.Select((c, i) => (c, i)).Where(x => char.IsDigit(x.c)).Select(x => x.i)];
        string emailDigits = new([.. digitPositions.Select(i => email.Body[i])]);
        foreach (ExtractedItem item in extraction.Items)
        {
            if (item.Quantity is <= 0)
            {
                issues.Add($"{item.Title}: quantity must be positive");
            }
            if (item.SourceQuote is null || !ExtractionScorer.QuoteIsInEmail(item.SourceQuote, email.Body))
            {
                issues.Add($"{item.Title}: source quote is not in the email");
            }
            if (item.Isbn is null)
            {
                continue;
            }
            string isbn = new([.. item.Isbn.Where(char.IsDigit)]);
            if (!IsValidIsbn13(isbn))
            {
                issues.Add($"{item.Title}: {item.Isbn} is not a valid ISBN-13");
            }
            else if (emailDigits.IndexOf(isbn, StringComparison.Ordinal) is int at and >= 0)
            {
                // The ISBN must sit next to this item's title, not next to another book
                string? nearest = NearestTitle(extraction, email.Body, digitPositions[at]);
                if (nearest is not null && !string.Equals(nearest, item.Title, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add($"{item.Title}: ISBN {isbn} is written next to {nearest}");
                }
            }
            else
            {
                issues.Add($"{item.Title}: ISBN {isbn} is not in the email");
            }
        }
        return issues;
    }

    static string? NearestTitle(Extraction extraction, string body, int position) =>
        extraction.Items
            .Select(i => (i.Title, Distance: Occurrences(body, i.Title).Select(p => Math.Abs(p - position)).DefaultIfEmpty(int.MaxValue).Min()))
            .Where(x => x.Distance != int.MaxValue)
            .OrderBy(x => x.Distance)
            .Select(x => x.Title)
            .FirstOrDefault();

    static IEnumerable<int> Occurrences(string body, string title)
    {
        for (int i = body.IndexOf(title, StringComparison.OrdinalIgnoreCase); i >= 0;
             i = body.IndexOf(title, i + 1, StringComparison.OrdinalIgnoreCase))
        {
            yield return i;
        }
    }

    public static bool IsValidIsbn13(string digits) =>
        digits.Length == 13
        && digits.All(char.IsAsciiDigit)
        && digits.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum() % 10 == 0;
}
