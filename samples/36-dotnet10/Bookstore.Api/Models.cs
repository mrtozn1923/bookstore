using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api;

public record CreateBookRequest(
    [property: Required, StringLength(100)] string Title,
    [property: Range(0.01, 100000)] decimal Price,
    string? Description);

public record BookDto(int Id, string Title, decimal Price, string? Description);

public record OrderStatus(int OrderId, string Status);

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
