using System.Globalization;

namespace Bookstore.Catalog;

// ISBN-13 to ISBN-10 for display. Only ISBN-13s with the 978 prefix have an ISBN-10; 979 ones do not.
public static class Isbn
{
    public static string? ToIsbn10(string isbn13)
    {
        if (isbn13.Length != 13 || !isbn13.All(char.IsAsciiDigit) || !isbn13.StartsWith("978") || !HasValidCheckDigit(isbn13))
        {
            return null;
        }

        string core = isbn13[3..12];
        int sum = 0;
        for (int i = 0; i < core.Length; i++)
        {
            sum += (10 - i) * (core[i] - '0');
        }
        int check = (11 - sum % 11) % 11;
        return core + (check == 10 ? "X" : check.ToString(CultureInfo.InvariantCulture));
    }

    private static bool HasValidCheckDigit(string isbn13)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (isbn13[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }
        return (10 - sum % 10) % 10 == isbn13[12] - '0';
    }
}
