using Bookstore.Catalog;

namespace Bookstore.Catalog.Tests;

public class BookTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);
    private static readonly DateOnly NextMonth = new(2026, 12, 1);

    private static readonly string[] Names =
        ["Dune paperback", "Middlemarch hardcover", "signed Hobbit", "Dune preorder", "Emma epub", "Emma preorder"];

    private static object Describe(string name) => Show(name switch
    {
        "Dune paperback" => new Book("9780441013593", "Dune", 120m, new PrintedFormat(450)),
        "Middlemarch hardcover" => new Book("9780141439549", " Middlemarch ", 189.50m, new PrintedFormat(1250)),
        "signed Hobbit" => new Book("9780261103344", "The Hobbit", 349.90m, new PrintedFormat(700) { SignedBy = "Grace" }),
        "Dune preorder" => new Book("9780441013593", "Dune", 120m, new PrintedFormat(450)) { ReleaseDate = NextMonth },
        "Emma epub" => new Book("9780141439587", "Emma", 59.90m, new DigitalFormat("EPUB")),
        "Emma preorder" => new Book("9780141439587", "Emma", 59.90m, new DigitalFormat("EPUB")) { ReleaseDate = NextMonth },
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    });

    private static object Show(Book b)
    {
        PrintedFormat? printed = b.Format as PrintedFormat;
        return new { b.Title, PriceWithVat = b.PriceWithVat(), WeightKg = printed?.ShippingWeightKg(), Delivery = b.Delivery(Today), printed?.SignedBy };
    }

    [Fact]
    public Task All_books() => Verify(Names.ToDictionary(n => n, Describe));

    [Fact]
    public void A_book_needs_a_title() =>
        Assert.Throws<ArgumentException>(() => new Book("9780141439587", " ", 59.90m, new DigitalFormat("EPUB")));
}
