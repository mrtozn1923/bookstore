using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api;

public record Book(int Id, string Title, string Isbn, decimal Price);

public record BookDto(int Id, string Title, string Isbn, decimal Price);

public class CreateBookRequest
{
    [Required, StringLength(100)]
    public string Title { get; set; } = "";

    [Required, UniqueIsbn]
    public string Isbn { get; set; } = "";

    [Range(0.01, 100000)]
    public decimal Price { get; set; }
}

public record PriceRange(decimal Min, decimal Max);
public union PriceFilter(decimal, PriceRange);
public record BookSearch(PriceFilter Price);

public record OrderStatus(int OrderId, string Status);
