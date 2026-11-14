using Bookstore.Catalog;

namespace Bookstore.Catalog.Isbn.Tests;

public sealed class Isbn13Tests
{
    [Test]
    [Arguments("9780441172719", true)]
    [Arguments("9780441172718", false)]
    [Arguments("978044117271", false)]
    [Arguments("1234567890123", false)]
    public async Task Isbn_is_validated(string isbn, bool expected)
    {
        await Assert.That(Isbn13.IsValid(isbn)).IsEqualTo(expected);
    }

    [Test]
    public async Task Check_digit_of_dune_is_nine()
    {
        int digit = Isbn13.CheckDigit("978044117271");

        await Assert.That(digit).IsEqualTo(9);
    }

    [Test]
    public async Task Prefix_must_have_twelve_digits()
    {
        await Assert.That(() => Isbn13.CheckDigit("978044117")).ThrowsExactly<ArgumentException>();
    }
}
