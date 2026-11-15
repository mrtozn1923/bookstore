using Bookstore.Admin.Client.Shared;
using Bookstore.Admin.Validation;

namespace Bookstore.Admin.Tests;

public sealed class SharedComponentTests : BunitContext
{
    [Fact]
    public void Money_formats_with_the_cascaded_panel_options()
    {
        IRenderedComponent<Money> money = Render<Money>(p => p
            .Add(m => m.Amount, 489m)
            .AddCascadingValue(new PanelOptions(Currency: "TRY", AmountFormat: "N2")));

        Assert.Equal($"{489m:N2} TRY", money.Markup.Trim());
    }

    [Theory]
    [InlineData("9780441172719", true)]
    [InlineData("978-0-15-662870-9", true)]
    [InlineData("9780156907391", false)]   // wrong check digit
    [InlineData("978015690739", false)]    // 12 digits
    [InlineData("", true)]                 // left to [Required]
    public void Isbn13_checks_length_and_check_digit(string isbn, bool valid) =>
        Assert.Equal(valid, new Isbn13Attribute().IsValid(isbn));
}
