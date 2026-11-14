using System.Text.RegularExpressions;
using Bookstore.Text;
using Xunit;

namespace Bookstore.Text.Tests;

public class BookstorePatternsTests
{
    [Theory]
    [InlineData("BKS-2026-000123", true)]
    [InlineData("BKS-2026-000123\n", false)]   // trailing newline, the $ trap from the first post
    [InlineData("BKS-2026-٠٠٠١٢٣", false)]      // Arabic-Indic digits
    [InlineData("bks-2026-000123", false)]
    [InlineData("BKS-26-000123", false)]
    [InlineData(" BKS-2026-000123", false)]
    public void OrderNumber(string input, bool expected) =>
        Assert.Equal(expected, BookstorePatterns.IsOrderNumber(input));

    [Theory]
    [InlineData("Ada Lovelace", true)]
    [InlineData("Ayşe Gül", true)]
    [InlineData("Jean-Luc", true)]
    [InlineData("O'Brien", true)]
    [InlineData("Ada  Lovelace", false)]
    [InlineData("Ada1", false)]
    [InlineData(" Ada", false)]
    [InlineData("", false)]
    public void CustomerName(string input, bool expected) =>
        Assert.Equal(expected, BookstorePatterns.IsCustomerName(input));

    [Fact]
    public void CustomerName_LongNearMiss_FinishesQuickly() =>
        Assert.False(BookstorePatterns.IsCustomerName(new string('a', 99) + "!"));

    // a regression test for the pattern we replaced: it must keep failing with a timeout
    [Fact]
    public void NaiveNamePattern_TimesOut() =>
        Assert.Throws<RegexMatchTimeoutException>(() =>
            Regex.IsMatch(new string('a', 30) + "!", @"^([a-zA-Z]+\s?)*$", RegexOptions.None, TimeSpan.FromMilliseconds(50)));
}
