namespace Bookstore.Ordering;

public interface IPaymentOption
{
    string Label { get; }
    decimal Fee(decimal subtotal);
    string ConfirmationNote(decimal subtotal);
}

public sealed class CardPayment : IPaymentOption
{
    public string Label => "Credit or debit card";
    public decimal Fee(decimal subtotal) => 0m;
    public string ConfirmationNote(decimal subtotal) => "Your card has been charged.";
}

public sealed class BankTransferPayment : IPaymentOption
{
    public string Label => "Bank transfer";
    public decimal Fee(decimal subtotal) => 0m;
    public string ConfirmationNote(decimal subtotal) => "Your order ships after we receive the transfer.";
}

public sealed class CashOnDeliveryPayment : IPaymentOption
{
    private const decimal FreeAbove = 1000m;
    private const decimal CourierFee = 15m;

    public string Label => "Cash on delivery";
    public decimal Fee(decimal subtotal) => subtotal >= FreeAbove ? 0m : CourierFee;
    public string ConfirmationNote(decimal subtotal) =>
        FormattableString.Invariant($"Please have {subtotal + Fee(subtotal):0.00} TRY ready for the courier.");
}

public static class PaymentOptions
{
    public static IPaymentOption For(PaymentMethod method) => method switch
    {
        PaymentMethod.Card => new CardPayment(),
        PaymentMethod.BankTransfer => new BankTransferPayment(),
        PaymentMethod.CashOnDelivery => new CashOnDeliveryPayment(),
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown payment method."),
    };
}
