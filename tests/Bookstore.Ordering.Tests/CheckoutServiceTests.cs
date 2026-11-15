namespace Bookstore.Ordering.Tests;

public sealed class CheckoutServiceTests
{
    private static CheckoutService NewService() =>
        new(new Dictionary<string, Coupon> { ["KITAP10"] = new("KITAP10", 10m, 200m, MaxDiscount: 100m) });

    private static readonly CartLine Dune = new("9780441013593", 2, 150m);

    [Fact]
    public void Order_with_a_coupon_gets_the_discount()
    {
        CheckoutResult result = NewService().PlaceOrder(new(CustomerType.Individual, [Dune], "KITAP10"));

        Assert.Equal(new PlacedOrder(1001, 300m, 30m, 270m), result.Order);
    }

    [Fact]
    public void Unknown_coupon_is_rejected()
    {
        CheckoutResult result = NewService().PlaceOrder(new(CustomerType.Individual, [Dune], "NOPE"));

        Assert.Null(result.Order);
        Assert.Equal("Unknown coupon 'NOPE'.", result.Error);
    }

    // Exploratory session findings, as regression tests
    [Fact]
    public void Empty_cart_is_rejected()
    {
        CheckoutResult result = NewService().PlaceOrder(new(CustomerType.Guest, [], null));

        Assert.Null(result.Order);
        Assert.Equal("Cart is empty.", result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Line_quantity_must_be_positive(int quantity)
    {
        CheckoutResult result = NewService().PlaceOrder(
            new(CustomerType.Guest, [new CartLine("9780141439518", quantity, 95m)], null));

        Assert.Null(result.Order);
        Assert.Equal("Quantity must be positive.", result.Error);
    }

    [Fact]
    public void Same_idempotency_key_returns_the_same_order()
    {
        CheckoutService service = NewService();
        CheckoutCommand command = new(CustomerType.Individual, [Dune], "KITAP10");

        CheckoutResult first = service.PlaceOrder(command, idempotencyKey: "checkout-7f3a");
        CheckoutResult second = service.PlaceOrder(command, idempotencyKey: "checkout-7f3a");

        Assert.Equal(first.Order, second.Order);
    }
}
