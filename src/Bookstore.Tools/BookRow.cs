using System.Globalization;

namespace Bookstore.Tools;

public sealed record BookRow(string Isbn, string Title, string Author, decimal PriceTry)
{
    public static BookRow Parse(string line)
    {
        string[] parts = line.Split(',');
        return new BookRow(parts[0], parts[1], parts[2], decimal.Parse(parts[3], CultureInfo.InvariantCulture));
    }

    public bool HasValidIsbn13()
    {
        if (Isbn.Length != 13 || !Isbn.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 13; i++)
        {
            int digit = Isbn[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        return sum % 10 == 0;
    }
}
