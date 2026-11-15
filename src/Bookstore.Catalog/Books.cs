namespace Bookstore.Catalog;

public sealed class Book
{
    public Book(string isbn, string title, decimal price, IBookFormat format)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A book needs a title.", nameof(title));
        Isbn = isbn;
        Title = title.Trim();
        Price = price;
        Format = format;
    }

    public string Isbn { get; }
    public string Title { get; }
    public decimal Price { get; }
    public DateOnly? ReleaseDate { get; init; }
    public IBookFormat Format { get; }

    public decimal VatRate => Format.VatRate;

    public decimal PriceWithVat() => Math.Round(Price * (1 + VatRate), 2, MidpointRounding.AwayFromZero);

    public string Delivery(DateOnly today) => Format.Delivery(ReleaseDate, today);
}
