using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

public sealed class CartPricingTests
{
    [Fact]
    public void Individual_customer_pays_the_sum_of_rounded_line_totals()
    {
        PriceQuote quote = CartPricing.Quote(
            [new PriceLine("9780441172719", 489.00m, 1), new PriceLine("9780261102217", 329.90m, 3)], institutional: false);

        Assert.Equal(new PriceQuote(1478.70m, 0m, 1478.70m), quote);
    }

    [Theory]
    [InlineData(19, 0)]
    [InlineData(20, 49.98)]
    public void Institution_gets_ten_percent_from_twenty_copies(int quantity, double expectedDiscount)
    {
        PriceQuote quote = CartPricing.Quote([new PriceLine("9780141439587", 24.99m, quantity)], institutional: true);

        Assert.Equal((decimal)expectedDiscount, quote.Discount);
        Assert.Equal(quote.Subtotal - quote.Discount, quote.Total);
    }
}
