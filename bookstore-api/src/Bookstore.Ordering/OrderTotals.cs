namespace Bookstore.Ordering;

public static class OrderTotals
{
	public static decimal RoundToKurus(decimal amount)
	{
		return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
	}
}
