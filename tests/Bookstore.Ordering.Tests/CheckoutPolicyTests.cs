using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class CheckoutPolicyTests
{
    private static readonly CartLine Dune = new("9780441013593", "Dune", 120m, 1, false);
    private static readonly CartLine Hobbit = new("9780261103344", "The Hobbit", 149.90m, 2, false);
    private static readonly CartLine TwelveEmmas = new("9780141439587", "Emma", 59.90m, 12, true);

    private static Cart NewCart(string name, PaymentMethod payment) => name switch
    {
        "guest, empty" => new Cart(Customer.CreateGuest(), [], payment),
        "guest, Dune" => new Cart(Customer.CreateGuest(), [Dune], payment),
        "Ada, empty" => new Cart(new Customer { Id = 1, Name = "Ada" }, [], payment),
        "Ada, Dune" => new Cart(new Customer { Id = 1, Name = "Ada", LoyaltyPoints = 40 }, [Dune], payment),
        "Ada, Hobbit x2" => new Cart(new Customer { Id = 1, Name = "Ada" }, [Hobbit], payment),
        "Ada, 12 Emmas" => new Cart(new Customer { Id = 1, Name = "Ada" }, [TwelveEmmas], payment),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static readonly string[] CartNames =
        ["guest, empty", "guest, Dune", "Ada, empty", "Ada, Dune", "Ada, Hobbit x2", "Ada, 12 Emmas"];

    [Fact]
    public Task Policy_for_all_carts() =>
        Combination()
            .Verify(
                (string name, PaymentMethod payment, DateOnly today) =>
                {
                    Cart cart = NewCart(name, payment);
                    CheckoutPolicy policy = new();
                    return new
                    {
                        Greeting = policy.Greeting(cart),
                        Badge = policy.LoyaltyBadge(cart),
                        Discount = policy.Discount(cart, today),
                        Error = policy.CheckoutError(cart),
                        Fee = policy.PaymentFee(cart),
                        Label = policy.PaymentLabel(payment),
                        Note = policy.ConfirmationNote(cart)
                    };
                },
                CartNames,
                Enum.GetValues<PaymentMethod>(),
                [new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 25)]);

    [Fact]
    public void Registered_customer_collects_points()
    {
        Cart cart = NewCart("Ada, Dune", PaymentMethod.Card);

        int points = new CheckoutPolicy().AwardLoyaltyPoints(cart);

        Assert.Equal(12, points);
        Assert.Equal(52, cart.Customer!.LoyaltyPoints);
    }

    [Fact]
    public void Guest_is_told_how_many_points_they_would_earn() =>
        Assert.Equal(12, new CheckoutPolicy().AwardLoyaltyPoints(NewCart("guest, Dune", PaymentMethod.Card)));

    [Fact]
    public void Guest_badge_stays_at_zero_after_checkout()
    {
        Cart cart = NewCart("guest, Dune", PaymentMethod.Card);
        CheckoutPolicy policy = new();

        policy.AwardLoyaltyPoints(cart);

        Assert.Equal("0 points", policy.LoyaltyBadge(cart));
    }

    [Fact]
    public void Negative_price_is_rejected_at_the_boundary() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new CartLine("9780441013593", "Dune", -120m, 1, false));
}
