using System.Globalization;
using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

public sealed class OrderItemTests
{
    [Theory]
    [InlineData("329.90", 2, "659.80")]
    [InlineData("0.125", 1, "0.13")]   // midpoint goes away from zero
    [InlineData("0.145", 1, "0.15")]   // banker's rounding would give 0.14
    public void Line_total_is_rounded_to_two_decimals(string unitPrice, int quantity, string expected)
    {
        decimal total = OrderItem.CalculateLineTotal(decimal.Parse(unitPrice, CultureInfo.InvariantCulture), quantity);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), total);
    }
}
