namespace Bookstore.Web.Catalog;

public enum BookFormat { Hardcover, Paperback, Ebook }

public sealed record Book(
    string Isbn,
    string Slug,
    string Title,
    string Author,
    string Genre,
    BookFormat Format,
    string Language,
    decimal Price,
    int Year)
{
    public DateOnly ContentUpdatedAt { get; init; } = new(2026, 11, 1);
}

public sealed record Genre(string Slug, string NameTr, string NameEn);
