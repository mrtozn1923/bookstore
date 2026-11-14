using System.Security.Cryptography;

namespace Bookstore.Catalog;

// Campaign coupon codes such as "WINTER25" or generated ones such as "K7QX2MPA9R".
public static class CouponCode
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string NewCode() => RandomNumberGenerator.GetString(Alphabet, 10);

    public static bool Matches(string input, string code) =>
        string.Equals(input.Trim(), code, StringComparison.OrdinalIgnoreCase);
}
