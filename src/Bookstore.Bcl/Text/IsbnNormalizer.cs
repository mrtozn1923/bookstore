using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Bookstore.Bcl.Text;

public static class IsbnNormalizer
{
    private static readonly SearchValues<char> Separators = SearchValues.Create(" -");

    // Accepts "ISBN 978-0-441-17271-9", "isbn: 978 0441 172719", "9780441172719".
    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? isbn13)
    {
        isbn13 = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        ReadOnlySpan<char> span = input.AsSpan().Trim();
        if (span.StartsWith("ISBN", StringComparison.OrdinalIgnoreCase))
            span = span[4..].TrimStart(": ");

        Span<char> digits = stackalloc char[13];
        int count = 0;
        foreach (char c in span)
        {
            if (Separators.Contains(c))
                continue;
            if (!char.IsAsciiDigit(c) || count == digits.Length)
                return false;
            digits[count++] = c;
        }

        if (count != digits.Length || !HasValidCheckDigit(digits))
            return false;

        isbn13 = new string(digits);
        return true;
    }

    // ISBN-13: digits are weighted 1,3,1,3...; the total must be a multiple of 10.
    private static bool HasValidCheckDigit(ReadOnlySpan<char> digits)
    {
        int sum = 0;
        for (int i = 0; i < digits.Length; i++)
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return sum % 10 == 0;
    }
}
