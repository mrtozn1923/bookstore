using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Dtos;

public class CreateBookRequest
{
    /// <summary>
    /// Title of the book.
    /// </summary>
    /// <example>Middlemarch</example>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Price in TRY.
    /// </summary>
    /// <example>390</example>
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    /// <summary>
    /// Id of an existing category.
    /// </summary>
    /// <example>2</example>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
