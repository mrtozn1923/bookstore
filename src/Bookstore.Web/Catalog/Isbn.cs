namespace Bookstore.Web.Catalog;

public static class Isbn
{
    // ISBN-13: weights 1,3,1,3...; the 13th digit makes the sum a multiple of 10
    public static bool IsValid13(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsAsciiDigit))
        {
            return false;
        }
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }
        return (10 - sum % 10) % 10 == isbn[12] - '0';
    }
}
