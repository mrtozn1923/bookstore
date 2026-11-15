namespace Bookstore.Ordering.Tests;

public sealed class CartTests
{
    // 1-50 lines are valid: empty cart, 49 -> 50 and 50 -> 51
    [Theory]
    [InlineData(0, true)]
    [InlineData(49, true)]
    [InlineData(50, false)]
    public void Cart_holds_at_most_50_lines(int existingLines, bool accepted)
    {
        Cart cart = new();
        for (int i = 0; i < existingLines; i++)
        {
            cart.TryAdd(new OrderLine($"isbn-{i}", 100m, 1));
        }

        bool added = cart.TryAdd(new OrderLine("9780441172719", 250m, 1));

        Assert.Equal(accepted, added);
        Assert.Equal(accepted ? existingLines + 1 : existingLines, cart.Lines.Count);
    }
}
