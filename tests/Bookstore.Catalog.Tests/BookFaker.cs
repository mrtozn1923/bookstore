using Bogus;

namespace Bookstore.Catalog.Tests;

public static class BookFaker
{
    private static readonly DateTime Reference = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Faker<Book> Create(int seed, string locale = "en") =>
        new Faker<Book>(locale)
            .UseSeed(seed)
            .UseDateTimeReference(Reference)
            .StrictMode(true)
            .RuleFor(b => b.Isbn, f => NewIsbn(f))
            .RuleFor(b => b.Title, f => f.Lorem.Sentence(f.Random.Int(1, 4)).TrimEnd('.'))
            .RuleFor(b => b.Author, f => f.Name.FullName())
            .RuleFor(b => b.Pages, f => f.Random.Int(80, 900))
            .RuleFor(b => b.Price, (f, b) => Math.Round(5m + b.Pages * 0.04m + f.Random.Decimal(0m, 10m), 2))
            .RuleFor(b => b.PublishedOn, f => f.Date.PastDateOnly(40))
            .RuleFor(b => b.Language, _ => locale)
            // New rules go last: values of the earlier rules stay the same for a given seed
            .RuleFor(b => b.Subtitle, f => f.Random.Bool(0.3f) ? f.Lorem.Sentence(3).TrimEnd('.') : null)
            .RuleSet("classic", rules => rules
                .RuleFor(b => b.PublishedOn, f => f.Date.PastDateOnly(150, new DateOnly(1950, 1, 1))));

    public static string NewIsbn(Faker f)
    {
        string first12 = f.PickRandom("978", "979") + f.Random.ReplaceNumbers("#########");
        return first12 + Isbn13.CheckDigit(first12);
    }
}
