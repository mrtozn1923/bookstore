using System.Globalization;
using static Bookstore.Ordering.Tests.Books;

namespace Bookstore.Ordering.Tests;

public sealed class ReceiptTests
{
    [Fact]
    public void Customer_email_is_trimmed_and_lower_cased()
    {
        Order order = DuneAndHobbit();

        Assert.Equal("ada@example.com", order.CustomerEmail);
    }

    [Fact]
    public void Order_number_has_year_and_six_digit_sequence()
    {
        string number = OrderNumber.Format(2026, 123);

        Assert.Matches(@"^BS-\d{4}-\d{6}$", number);
        Assert.StartsWith("BS-2026-", number);
    }

    [Fact]
    public void Receipt_lists_lines_and_total()
    {
        string expected = """
            Order BS-2026-000123
            9780441172719 x2 25.00
            9780261103344 x1 9.90
            Total 34.90
            """;

        string receipt = Receipt.Render(DuneAndHobbit());

        Assert.Equal(expected, receipt, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    [Fact]
    public void Total_is_formatted_in_turkish_lira()
    {
        Assert.SkipUnless(CultureDataAvailable("tr-TR"), "tr-TR culture data is not available (globalization-invariant mode).");

        string total = Receipt.FormatTotal(DuneAndHobbit(), "tr-TR");

        Assert.Equal("₺34,90", total);
    }

    private static bool CultureDataAvailable(string name)
    {
        try
        {
            _ = CultureInfo.GetCultureInfo(name);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    [Fact]
    public void Isbn_prefix_is_checked_without_allocating()
    {
        ReadOnlySpan<char> isbn = Dune.AsSpan();

        Assert.StartsWith("978", isbn);
        Assert.Equal("0441172719", isbn[3..]);
    }
}
