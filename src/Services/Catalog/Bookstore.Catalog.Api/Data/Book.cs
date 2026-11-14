namespace Bookstore.Catalog.Data;

public sealed class Book
{
    public int Id { get; set; }
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int PublishedYear { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
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
