namespace Bookstore.Data;

public sealed class Book
{
    public required string Isbn { get; init; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
}
