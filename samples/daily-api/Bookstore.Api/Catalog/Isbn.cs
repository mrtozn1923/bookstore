using System.Diagnostics.CodeAnalysis;

namespace Bookstore.Api.Catalog;

public readonly record struct Isbn(string Value) : IParsable<Isbn>
{
    // "978-0441013593" ve "9780441013593" aynı ISBN; tireleri atıp checksum'a bakıyoruz
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Isbn result)
    {
        result = default;
        string digits = (s ?? "").Replace("-", "");
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        if ((10 - sum % 10) % 10 != digits[12] - '0')
        {
            return false;
        }

        result = new Isbn(digits);
        return true;
    }

    public static Isbn Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out Isbn result) ? result : throw new FormatException($"'{s}' is not a valid ISBN-13.");

    public override string ToString() => Value;
}
