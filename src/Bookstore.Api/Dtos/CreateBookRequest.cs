using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Dtos;

public class CreateBookRequest
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
