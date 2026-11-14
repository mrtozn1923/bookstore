namespace Bookstore.Api;

public class FlashSaleService(TimeProvider timeProvider)
{
    private static readonly TimeSpan SaleStart = TimeSpan.FromHours(20);
    private static readonly TimeSpan SaleEnd = TimeSpan.FromHours(22);

    public bool IsActive()
    {
        var now = timeProvider.GetUtcNow().TimeOfDay;
        return now >= SaleStart && now < SaleEnd;
    }
}
