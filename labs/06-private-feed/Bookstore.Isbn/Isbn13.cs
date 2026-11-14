namespace Bookstore.Isbn;

public static class Isbn13
{
    public static bool IsValid(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 13; i++)
        {
            int digit = isbn[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        return sum % 10 == 0;
    }
}
