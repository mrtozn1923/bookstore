using static Bookstore.Ordering.DiscountCalculator;

namespace Bookstore.Ordering.Pricing;

public sealed class CampaignCalculator
{
	public decimal ApplyCampaign(decimal subtotal, bool isCampaignActive)
	{
		if (isCampaignActive)
		{
			var discounted = subtotal - subtotal * InstitutionalRate;
			return OrderTotals.RoundToKurus(discounted);
		}

		return subtotal;
	}
}
