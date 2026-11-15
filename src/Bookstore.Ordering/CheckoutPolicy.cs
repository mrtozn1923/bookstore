using System.Diagnostics;

namespace Bookstore.Ordering;

public enum PaymentMethod
{
    Card,
    BankTransfer,
    CashOnDelivery
}

public sealed record Cart(Customer Customer, IReadOnlyList<CartLine> Lines, PaymentMethod Payment)
{
    public decimal Subtotal => Lines.Sum(l => l.UnitPrice * l.Quantity);
}

public class CheckoutPolicy
{
    private const decimal CampaignMinimum = 250m;

    public string Greeting(Cart cart) =>
        cart.Customer.IsGuest ? "Welcome, guest!" : $"Welcome back, {cart.Customer.Name}!";

    public int AwardLoyaltyPoints(Cart cart)
    {
        int points = (int)(cart.Subtotal / 10);
        cart.Customer.EarnPoints(points);
        return points;
    }

    public string LoyaltyBadge(Cart cart) => $"{cart.Customer.LoyaltyPoints} points";

    public decimal Discount(Cart cart, DateOnly today)
    {
        if (!IsEligibleForDiscount(cart))
            return 0m;

        decimal discount = QualifiesForCampaign(cart, today) ? CampaignDiscount(cart) : RegularDiscount(cart);
        Debug.Assert(discount >= 0 && discount <= cart.Subtotal, "A discount is never negative and never exceeds the subtotal.");
        return discount;
    }

    private static bool IsEligibleForDiscount(Cart cart) =>
        cart is not ({ Customer.IsGuest: true } or { Lines.Count: 0 } or { Payment: PaymentMethod.CashOnDelivery });

    private static bool QualifiesForCampaign(Cart cart, DateOnly today) =>
        IsCampaignWeek(today) && cart.Subtotal >= CampaignMinimum;

    private static bool IsCampaignWeek(DateOnly today) =>
        today is { Month: 11, Day: >= 24 and <= 30 };

    private static decimal CampaignDiscount(Cart cart) => Round(cart.Subtotal * 0.15m);

    private static decimal RegularDiscount(Cart cart) => Round(cart.Subtotal * 0.05m);

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public string? CheckoutError(Cart cart)
    {
        if (cart.Lines.Count == 0)
            return "The cart is empty.";
        if (cart.Customer.IsGuest && cart.Payment == PaymentMethod.BankTransfer)
            return "Guests cannot pay by bank transfer.";

        if (cart.Lines.Any(l => l.Quantity > OrderSettings.MaxQuantityPerLine))
            return $"At most {OrderSettings.MaxQuantityPerLine} copies per line.";
        return null;
    }

    public decimal PaymentFee(Cart cart) => PaymentOptions.For(cart.Payment).Fee(cart.Subtotal);

    public string PaymentLabel(PaymentMethod method) => PaymentOptions.For(method).Label;

    public string ConfirmationNote(Cart cart) => PaymentOptions.For(cart.Payment).ConfirmationNote(cart.Subtotal);
}
