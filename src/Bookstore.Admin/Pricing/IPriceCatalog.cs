using Bookstore.Bcl.Text;

namespace Bookstore.Admin.Pricing;

public interface IPriceCatalog
{
    Task<decimal> UpdatePriceAsync(Isbn isbn, decimal newPrice, CancellationToken cancellationToken);
}

// Stands in for Bookstore's catalog API in this post: every update is a 50 ms "network call".
public sealed class SimulatedPriceCatalog : IPriceCatalog
{
    private readonly Dictionary<Isbn, decimal> _prices = [];

    public async Task<decimal> UpdatePriceAsync(Isbn isbn, decimal newPrice, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        decimal oldPrice = _prices.GetValueOrDefault(isbn, 299.90m);
        _prices[isbn] = newPrice;
        return oldPrice;
    }
}
