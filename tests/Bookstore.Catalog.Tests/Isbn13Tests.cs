namespace Bookstore.Catalog.Tests;

public sealed class Isbn13Tests
{
    // Digit count: 0-12 invalid, 13 valid, 14+ invalid; 11..15 with 3-value BVA
    [Theory]
    [InlineData("97804411727", false)]
    [InlineData("978044117271", false)]
    [InlineData("9780441172719", true)]
    [InlineData("97804411727190", false)]
    [InlineData("978044117271900", false)]
    [InlineData("978-0-441-17271-9", true)]
    [InlineData("", false)]
    public void Isbn_must_have_exactly_13_digits(string value, bool valid)
    {
        Assert.Equal(valid, Isbn13.IsValid(value));
    }
}
