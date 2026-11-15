using System.Globalization;
using System.Text.RegularExpressions;

namespace Bookstore.Prompts.Tasks.OrderExtraction;

public sealed record DateResolution(DateOnly? Date, bool Ambiguous);

// Turns the phrase the model copied ("gelecek salıya kadar") into a date, in code
public static partial class TurkishDateResolver
{
    static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static readonly (string Stem, DayOfWeek Day)[] Days =
    [
        ("pazartesi", DayOfWeek.Monday), ("salı", DayOfWeek.Tuesday), ("çarşamba", DayOfWeek.Wednesday),
        ("perşembe", DayOfWeek.Thursday), ("cumartesi", DayOfWeek.Saturday), ("cuma", DayOfWeek.Friday),
        ("pazar", DayOfWeek.Sunday),
    ];

    static readonly string[] Months =
        ["ocak", "şubat", "mart", "nisan", "mayıs", "haziran", "temmuz", "ağustos", "eylül", "ekim", "kasım", "aralık"];

    public static DateResolution Resolve(string? phrase, DateOnly sent)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return new(null, false);
        }
        string text = phrase.ToLower(Tr);

        if (text.Contains("yarın"))
        {
            return new(sent.AddDays(1), false);
        }

        Match dayOfMonth = DayOfMonthPattern().Match(text);
        if (dayOfMonth.Success)
        {
            DateOnly candidate = new(sent.Year, sent.Month, int.Parse(dayOfMonth.Groups[1].Value));
            return new(candidate < sent ? candidate.AddMonths(1) : candidate, false);
        }

        Match dayAndMonth = DayAndMonthPattern().Match(text);
        if (dayAndMonth.Success && Array.FindIndex(Months, m => dayAndMonth.Groups[2].Value.StartsWith(m)) is int month and >= 0)
        {
            DateOnly candidate = new(sent.Year, month + 1, int.Parse(dayAndMonth.Groups[1].Value));
            return new(candidate < sent ? candidate.AddYears(1) : candidate, false);
        }

        foreach ((string stem, DayOfWeek day) in Days)
        {
            if (!text.Contains(stem))
            {
                continue;
            }
            int ahead = ((int)day - (int)sent.DayOfWeek + 7) % 7;
            DateOnly next = sent.AddDays(ahead == 0 ? 7 : ahead);
            // "Gelecek cuma" said on a Monday: this Friday or the one after? Do not guess.
            bool sameWeek = ISOWeek.GetWeekOfYear(next.ToDateTime(TimeOnly.MinValue))
                == ISOWeek.GetWeekOfYear(sent.ToDateTime(TimeOnly.MinValue));
            bool saysNext = text.Contains("gelecek") || text.Contains("önümüzdeki") || text.Contains("haftaya");
            return saysNext && sameWeek ? new(null, true) : new(next, false);
        }

        return new(null, false);
    }

    [GeneratedRegex(@"ayın (\d{1,2})")]
    private static partial Regex DayOfMonthPattern();

    [GeneratedRegex(@"(\d{1,2}) ([a-zçğıöşü]+)")]
    private static partial Regex DayAndMonthPattern();
}
