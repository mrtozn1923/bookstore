namespace Bookstore.Catalog;

public static class Isbn13
{
    public const int Length = 13;

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string digits = value.Replace("-", "").Replace(" ", "");
        if (digits.Length != Length || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < Length - 1; i++)
        {
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        int checkDigit = (10 - sum % 10) % 10;
        return checkDigit == digits[Length - 1] - '0';
    }
}
