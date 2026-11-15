namespace Bookstore.Ordering;

public sealed class InvoiceCalculator(Order order, decimal discountRate, decimal vatRate)
{
    public decimal BaseAmount => order.Total;

    public decimal Discount => Round(BaseAmount * discountRate);

    public decimal Vat => Round((BaseAmount - Discount) * vatRate);

    public decimal Total => BaseAmount - Discount + Vat;

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
