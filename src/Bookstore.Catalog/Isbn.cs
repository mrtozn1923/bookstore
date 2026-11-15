using System.Diagnostics.CodeAnalysis;

namespace Bookstore.Catalog;

public sealed record Isbn
{
    private Isbn(string value) => Value = value;

    public string Value { get; }

    public static Isbn Parse(string text) =>
        TryParse(text, out Isbn? isbn) ? isbn : throw new FormatException($"'{text}' is not a valid ISBN-13.");

    public static bool TryParse(string? text, [NotNullWhen(true)] out Isbn? isbn)
    {
        isbn = null;
        string digits = (text ?? "").Replace("-", "").Replace(" ", "");
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
            return false;

        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        if ((10 - sum % 10) % 10 != digits[12] - '0')
            return false;

        isbn = new Isbn(digits);
        return true;
    }

    public override string ToString() => Value;
}
