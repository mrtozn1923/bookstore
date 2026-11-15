using System.Globalization;

namespace Bookstore.Ordering.Tests;

public sealed class CouponCodeTests
{
    // Error guessing: the Turkish I, upper/lower case and leading/trailing spaces
    [Theory]
    [InlineData("en-US", "kitap10")]
    [InlineData("tr-TR", "kitap10")]
    [InlineData("tr-TR", " Kitap10 ")]
    [InlineData("tr-TR", "KITAP10")]
    public void Coupon_code_is_normalized_the_same_way_in_every_culture(string culture, string code)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

            Assert.Equal("KITAP10", CouponCodes.Normalize(code));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
