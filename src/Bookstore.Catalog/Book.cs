namespace Bookstore.Catalog;

public sealed record Book
{
    public required string Isbn { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public required string Author { get; init; }

    public decimal Price { get; init; }

    public int Pages { get; init; }

    public DateOnly PublishedOn { get; init; }

    public string Language { get; init; } = "en";
}
