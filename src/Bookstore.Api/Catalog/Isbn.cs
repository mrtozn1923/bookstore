namespace Bookstore.Api.Catalog;

public static class Isbn
{
    public static bool IsValid(string? value)
    {
        if (value is null)
        {
            return false;
        }

        string digits = value.Replace("-", "");
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = digits[i] - '0';
            sum += i % 2 == 0 ? d : d * 3;
        }

        int check = (10 - sum % 10) % 10;
        return check == digits[12] - '0';
    }
}
