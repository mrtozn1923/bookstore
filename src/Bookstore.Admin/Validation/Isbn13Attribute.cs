using System.ComponentModel.DataAnnotations;

namespace Bookstore.Admin.Validation;

// ISBN-13: 13 digits (hyphens and spaces allowed) whose weighted sum (1, 3, 1, 3, ...) is a multiple of 10.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class Isbn13Attribute() : ValidationAttribute("{0} is not a valid ISBN-13.")
{
    public override bool IsValid(object? value)
    {
        if (value is not string text || text.Length == 0)
        {
            return true; // [Required] decides whether an empty value is allowed
        }

        string digits = Normalize(text);
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 13; i++)
        {
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }
        return sum % 10 == 0;
    }

    public static string Normalize(string isbn) => isbn.Replace("-", "").Replace(" ", "");
}
