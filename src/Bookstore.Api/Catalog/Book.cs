namespace Bookstore.Catalog;

public sealed class Book
{
    public int Id { get; set; }
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int PublishedYear { get; set; }

    // Blob name in the covers container; null until a cover is uploaded
    public string? CoverBlobName { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
