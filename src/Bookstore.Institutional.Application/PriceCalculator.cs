using Bookstore.Institutional.Domain;

namespace Bookstore.Institutional.Application;

public sealed class PriceCalculator : IPriceCalculator
{
    public Money CalculateTotal(Money listPrice, int quantity, DiscountAgreement agreement)
    {
        decimal listTotal = listPrice.Amount * quantity;
        return new Money(decimal.Round(listTotal * (1 - agreement.DiscountRate), 2), listPrice.Currency);
    }
}
