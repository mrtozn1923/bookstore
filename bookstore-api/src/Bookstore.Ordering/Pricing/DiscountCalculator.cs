namespace Bookstore.Ordering;

public sealed class DiscountCalculator
{
	public decimal ApplyDiscount(decimal subtotal, int quantity, bool isInstitutional)
	{
		if (isInstitutional && quantity > InstitutionalMinimumQuantity)
		{
			var discounted = subtotal - subtotal * InstitutionalRate;
			return OrderTotals.RoundToKurus(discounted);
		}

		return subtotal;
	}

	public const int InstitutionalMinimumQuantity = 20;
	public const decimal InstitutionalRate = 0.10m;
}
