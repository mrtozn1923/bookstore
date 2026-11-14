using Bogus;

namespace Bookstore.Catalog.Tests;

public sealed class BookFakerTests
{
    [Fact]
    public void Same_seed_gives_the_same_catalog()
    {
        List<Book> first = BookFaker.Create(seed: 2026).Generate(100);
        List<Book> second = BookFaker.Create(seed: 2026).Generate(100);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Catalog_of_1000_books_has_valid_and_unique_isbns()
    {
        List<Book> catalog = BookFaker.Create(seed: 2026).Generate(1_000);

        Assert.All(catalog, b => Assert.True(Isbn13.IsValid(b.Isbn), $"{b.Isbn} is not a valid ISBN-13"));
        Assert.Distinct(catalog.Select(b => b.Isbn));
    }

    [Fact]
    public void Generated_values_stay_inside_the_rules()
    {
        List<Book> catalog = BookFaker.Create(seed: 7).Generate(1_000);

        Assert.All(catalog, b =>
        {
            Assert.InRange(b.Pages, 80, 900);
            Assert.InRange(b.Price, 5m + b.Pages * 0.04m, 15m + b.Pages * 0.04m);
            Assert.InRange(b.PublishedOn, new DateOnly(1986, 11, 1), new DateOnly(2026, 11, 1));
        });
    }

    [Fact]
    public void Classic_rule_set_moves_publication_before_1950()
    {
        List<Book> classics = BookFaker.Create(seed: 2026).Generate(50, "default,classic");

        Assert.All(classics, b => Assert.True(b.PublishedOn < new DateOnly(1950, 1, 1)));
    }

    [Fact]
    public void Lazy_generation_creates_only_what_is_enumerated()
    {
        int created = 0;
        Faker<Book> books = BookFaker.Create(seed: 2026).FinishWith((_, _) => created++);

        List<Book> firstFive = [.. books.GenerateLazy(1_000).Take(5)];

        Assert.Equal(5, firstFive.Count);
        Assert.Equal(5, created);
    }

    [Fact]
    public void Strict_mode_lists_the_properties_without_a_rule()
    {
        Faker<Book> incomplete = new Faker<Book>()
            .StrictMode(true)
            .RuleFor(b => b.Isbn, f => BookFaker.NewIsbn(f))
            .RuleFor(b => b.Title, f => f.Lorem.Word())
            .RuleFor(b => b.Author, f => f.Name.FullName());

        ValidationException ex = Assert.Throws<ValidationException>(() => incomplete.Generate());

        Assert.Contains("Subtitle", ex.Message);
    }
}
