using Bookstore.Catalog;
using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

public sealed class CartPricingTests
{
    private static readonly Dictionary<int, BookInfo> Books = new()
    {
        [1] = new BookInfo(1, "9780441172719", "Dune", 489.00m, CategoryId: 2),
        [2] = new BookInfo(2, "9780261102217", "The Hobbit", 329.90m, CategoryId: 3)
    };

    [Fact]
    public void Lines_are_sorted_by_title_and_summed()
    {
        CartPrice price = CartPricing.Price(
            [new CartItem { BookId = 2, Quantity = 2 }, new CartItem { BookId = 1, Quantity = 1 }], Books);

        Assert.Equal(["Dune", "The Hobbit"], price.Lines.Select(l => l.Book.Title));
        Assert.Equal(659.80m, price.Lines[1].LineTotal);
        Assert.Equal(1148.80m, price.Subtotal);
    }

    [Fact]
    public void An_empty_cart_costs_nothing()
    {
        CartPrice price = CartPricing.Price([], Books);

        Assert.Empty(price.Lines);
        Assert.Equal(0m, price.Subtotal);
    }

    [Fact]
    public void Each_line_is_rounded_before_the_subtotal_is_summed()
    {
        Dictionary<int, BookInfo> books = new()
        {
            [1] = new BookInfo(1, "9780141439518", "Pride and Prejudice", 0.125m, CategoryId: 1),
            [2] = new BookInfo(2, "9780141439587", "Emma", 0.125m, CategoryId: 1)
        };

        CartPrice price = CartPricing.Price(
            [new CartItem { BookId = 1, Quantity = 1 }, new CartItem { BookId = 2, Quantity = 1 }], books);

        // 0.125 is rounded to 0.13 on each line; rounding only the sum would give 0.25.
        Assert.Equal(0.26m, price.Subtotal);
    }
}
