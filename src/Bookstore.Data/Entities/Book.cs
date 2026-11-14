namespace Bookstore.Data.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Slug { get; set; }
    public string Isbn { get; set; } = null!;
    public decimal Price { get; set; }
    public DateOnly PublishedOn { get; set; }
    public List<string> Tags { get; set; } = [];
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
