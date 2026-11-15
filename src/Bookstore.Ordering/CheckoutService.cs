namespace Bookstore.Ordering;

public class CheckoutService
{
    private const decimal PrintedBookVatRate = 0.10m;
    private const decimal EbookVatRate = 0.20m;
    private const decimal FreeShippingThreshold = 300m;
    private const decimal StandardShippingFee = 49.90m;
    private const decimal ExpressSurcharge = 35m;

    public CheckoutTotals CalculateTotals(IReadOnlyList<CartLine> lines, bool isExpress)
    {
        decimal subtotal = 0;
        decimal vat = 0;
        bool hasPrintedBooks = false;
        foreach (CartLine line in lines)
        {
            subtotal += LineTotal(line);
            vat += VatFor(line);
            if (!line.IsEbook)
                hasPrintedBooks = true;
        }

        decimal shipping = hasPrintedBooks ? ShippingFee(subtotal, isExpress) : 0;

        return new CheckoutTotals(subtotal, vat, shipping, subtotal + vat + shipping);
    }

    private static decimal ShippingFee(decimal subtotal, bool isExpress)
    {
        bool qualifiesForFreeShipping = subtotal >= FreeShippingThreshold;
        decimal fee = qualifiesForFreeShipping ? 0 : StandardShippingFee;
        return isExpress ? fee + ExpressSurcharge : fee;
    }

    private static decimal LineTotal(CartLine line) => line.UnitPrice * line.Quantity;

    private static decimal VatFor(CartLine line)
    {
        decimal rate = line.IsEbook ? EbookVatRate : PrintedBookVatRate;
        return Math.Round(LineTotal(line) * rate, 2, MidpointRounding.AwayFromZero);
    }
}
