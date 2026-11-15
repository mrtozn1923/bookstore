using System.ComponentModel.DataAnnotations;
using Bookstore.Admin.Validation;

namespace Bookstore.Admin.Components.Catalog;

// The root of the form's validation graph; NewBookForm is reached through Books.
[ValidatableType]
public sealed class NewAuthorForm : IValidatableObject
{
    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [MinLength(1, ErrorMessage = "Add at least one book.")]
    public List<NewBookForm> Books { get; set; } = [new()];

    // Runs after every property rule has passed.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        IEnumerable<string> duplicates = Books
            .GroupBy(b => Isbn13Attribute.Normalize(b.Isbn))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (string isbn in duplicates)
        {
            yield return new ValidationResult($"ISBN {isbn} appears more than once in this form.");
        }
    }
}

public sealed class NewBookForm
{
    [Required, Isbn13, Display(Name = "ISBN")]
    public string Isbn { get; set; } = "";

    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Range(typeof(decimal), "1", "10000", ErrorMessage = "The price must be between 1 and 10,000 TRY.")]
    public decimal Price { get; set; }

    [Range(1450, 2100), Display(Name = "Year")]
    public int PublishedYear { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    public string CategorySlug { get; set; } = "";
}
