using System.ComponentModel.DataAnnotations;

namespace Bookstore.MinimalApi.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true)]
    public decimal Price { get; set; }

    public int CategoryId { get; set; }
}
