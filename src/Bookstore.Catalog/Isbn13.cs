namespace Bookstore.Catalog;

// ISBN-13: 12 digits plus a check digit; weights alternate 1, 3, 1, 3...
public static class Isbn13
{
    public static int CheckDigit(ReadOnlySpan<char> first12)
    {
        if (first12.Length != 12)
        {
            throw new ArgumentException("An ISBN-13 prefix has 12 digits.", nameof(first12));
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = first12[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        return (10 - sum % 10) % 10;
    }

    public static bool IsValid(string? isbn) =>
        isbn is { Length: 13 }
        && isbn.All(char.IsAsciiDigit)
        && isbn.StartsWith("97", StringComparison.Ordinal)
        && CheckDigit(isbn.AsSpan(0, 12)) == isbn[12] - '0';
}
