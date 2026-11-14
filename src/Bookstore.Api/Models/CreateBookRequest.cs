using System.ComponentModel.DataAnnotations;
using Bookstore.Api.Validation;

namespace Bookstore.Api.Models;

public class CreateBookRequest : IValidatableObject
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^978-\d{10}$", ErrorMessage = "Isbn must look like 978-0441013593.")]
    public string Isbn { get; set; } = string.Empty;

    [AllowedFormat("hardcover", "paperback", "ebook")]
    public string Format { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true)]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int CategoryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountPrice is not null && DiscountPrice >= Price)
        {
            yield return new ValidationResult(
                "Discount price must be lower than price.",
                new[] { nameof(DiscountPrice) });
        }
    }
}
