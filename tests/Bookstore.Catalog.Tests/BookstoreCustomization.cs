using AutoFixture;

namespace Bookstore.Catalog.Tests;

// AutoFixture 4.18.1 cannot create DateOnly by itself: it calls the constructor with random year/month/day
public sealed class DateOnlyCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Customize<DateOnly>(c => c.FromFactory((DateTime d) => DateOnly.FromDateTime(d)));
}

// Anonymous books still need a valid ISBN and a real culture name
public sealed class BookCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Customize<Book>(c => c
            .With(b => b.Isbn, (int n) => ToIsbn(n))
            .With(b => b.Language, "en"));

    private static string ToIsbn(int n)
    {
        string first12 = "978" + (n % 1_000_000_000).ToString("D9");
        return first12 + Isbn13.CheckDigit(first12);
    }
}

public sealed class BookstoreCustomization() : CompositeCustomization(new DateOnlyCustomization(), new BookCustomization());
