using Bookstore.Contracts.Catalog;

namespace Bookstore.Catalog.Data;

public sealed class Book
{
    private readonly List<object> _events = [];

    public int Id { get; set; }
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int PublishedYear { get; set; }

    // Concurrency token and event version in one: every change increments it.
    public int Version { get; private set; } = 1;

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public IReadOnlyList<object> Events => _events;

    public void ChangePrice(decimal newPrice, TimeProvider clock)
    {
        if (newPrice == Price)
        {
            return;
        }
        decimal oldPrice = Price;
        Price = newPrice;
        Version++;
        _events.Add(new BookPriceChanged(Isbn, oldPrice, newPrice, Catalog.Currency, Version, clock.GetUtcNow()));
    }

    public void Rename(string newTitle, TimeProvider clock)
    {
        if (newTitle == Title)
        {
            return;
        }
        string oldTitle = Title;
        Title = newTitle;
        Version++;
        _events.Add(new BookRenamed(Isbn, oldTitle, newTitle, Version, clock.GetUtcNow()));
    }

    public void ClearEvents() => _events.Clear();
}

public sealed class Author
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Book> Books { get; } = [];
}

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
}

public static class Catalog
{
    public const string Currency = "TRY";
}
