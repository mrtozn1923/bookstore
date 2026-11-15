namespace Bookstore.Contracts;

/// <summary>A validated 13-digit International Standard Book Number.</summary>
public readonly record struct Isbn
{
    private Isbn(string value) => Value = value;

    /// <summary>The 13 digits without hyphens or spaces.</summary>
    public string Value { get; }

    /// <summary>Parses an ISBN-13, ignoring hyphens and spaces, and checks its check digit.</summary>
    public static bool TryParse(string? input, out Isbn isbn)
    {
        isbn = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string digits = input.Replace("-", "").Replace(" ", "");
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (IsbnChecksum.Compute(digits.AsSpan(0, 12)) != digits[12] - '0')
        {
            return false;
        }

        isbn = new Isbn(digits);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
