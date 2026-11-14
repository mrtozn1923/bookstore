using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Validation;

public class AllowedFormatAttribute : ValidationAttribute
{
    private readonly string[] _formats;

    public AllowedFormatAttribute(params string[] formats)
    {
        _formats = formats;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string format && _formats.Contains(format))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            $"{validationContext.DisplayName} must be one of: {string.Join(", ", _formats)}.",
            new[] { validationContext.MemberName! });
    }
}
