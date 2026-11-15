using System.Globalization;
using System.Text.Json;

namespace Bookstore.Prompts.Tasks.OrderExtraction;

public sealed record ExtractedItem(string Title, string? Isbn, int? Quantity, string? SourceQuote);

public sealed record Extraction(List<ExtractedItem> Items, string? DeliveryDateText, string? DeliveryDate);

// Field-level counts; a single pass/fail per email hides where the prompt is weak
public sealed class ExtractionScore
{
    public int ExpectedItems { get; set; }
    public int FoundItems { get; set; }
    public int ExtraItems { get; set; }
    public int QuantityCorrect { get; set; }
    public int QuantityInvented { get; set; }
    public int IsbnCorrect { get; set; }
    public int IsbnInvented { get; set; }
    public int Emails { get; set; }
    public int ModelDateCorrect { get; set; }
    public int CodeDateCorrect { get; set; }
    public int Quotes { get; set; }
    public int QuotesFound { get; set; }
    public List<string> Problems { get; } = [];
}

public static class ExtractionScorer
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Extraction? Parse(string output)
    {
        try
        {
            return JsonSerializer.Deserialize<Extraction>(output, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void Add(ExtractionScore score, OrderEmail email, ExpectedOrder expected, Extraction? actual, bool resolveInCode)
    {
        score.Emails++;
        score.ExpectedItems += expected.Items.Count;
        if (actual is null)
        {
            score.Problems.Add("not JSON");
            return;
        }

        List<ExtractedItem> unmatched = [.. actual.Items];
        foreach (ExpectedItem want in expected.Items)
        {
            ExtractedItem? got = unmatched.FirstOrDefault(i => SameTitle(i.Title, want.Title));
            if (got is null)
            {
                score.Problems.Add($"{want.Title}: missing");
                continue;
            }
            unmatched.Remove(got);
            score.FoundItems++;
            score.QuantityCorrect += got.Quantity == want.Quantity ? 1 : 0;
            score.QuantityInvented += want.Quantity is null && got.Quantity is not null ? 1 : 0;
            if (got.Quantity != want.Quantity)
            {
                score.Problems.Add($"{want.Title}: quantity {Show(want.Quantity)} -> {Show(got.Quantity)}");
            }
            string? isbn = DigitsOnly(got.Isbn);
            score.IsbnCorrect += isbn == want.Isbn ? 1 : 0;
            score.IsbnInvented += want.Isbn is null && isbn is not null ? 1 : 0;
            if (isbn != want.Isbn)
            {
                score.Problems.Add($"{want.Title}: isbn {want.Isbn ?? "null"} -> {isbn ?? "null"}");
            }
        }
        score.ExtraItems += unmatched.Count;
        score.Problems.AddRange(unmatched.Select(i => $"extra item {i.Title}"));

        foreach (ExtractedItem item in actual.Items.Where(i => i.SourceQuote is not null))
        {
            score.Quotes++;
            bool found = QuoteIsInEmail(item.SourceQuote!, email.Body);
            score.QuotesFound += found ? 1 : 0;
            if (!found)
            {
                score.Problems.Add($"{item.Title}: quote not in email");
            }
        }

        DateOnly? modelDate = DateOnly.TryParseExact(actual.DeliveryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateOnly parsed) ? parsed : null;
        DateResolution codeDate = TurkishDateResolver.Resolve(actual.DeliveryDateText, email.Sent);
        score.ModelDateCorrect += modelDate == expected.DeliveryDate ? 1 : 0;
        bool codeCorrect = codeDate.Date == expected.DeliveryDate && codeDate.Ambiguous == expected.DateAmbiguous;
        score.CodeDateCorrect += codeCorrect ? 1 : 0;
        // When code resolves the date, the model's own date is only a comparison column
        if (!resolveInCode && modelDate != expected.DeliveryDate)
        {
            score.Problems.Add($"date (model) {Show(expected.DeliveryDate)} -> {Show(modelDate)}");
        }
        if (resolveInCode && !codeCorrect)
        {
            score.Problems.Add($"date (code) from \"{actual.DeliveryDateText ?? "null"}\" -> {(codeDate.Ambiguous ? "ambiguous" : Show(codeDate.Date))}");
        }
    }

    static string Show(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "null";

    static string Show(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "null";

    public static bool QuoteIsInEmail(string quote, string body) =>
        Squash(body).Contains(Squash(quote), StringComparison.OrdinalIgnoreCase);

    static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    static bool SameTitle(string actual, string expected) =>
        actual.Contains(expected, StringComparison.OrdinalIgnoreCase);

    static string? DigitsOnly(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }
        string digits = new([.. isbn.Where(char.IsDigit)]);
        return digits.Length == 0 ? null : digits;
    }
}
