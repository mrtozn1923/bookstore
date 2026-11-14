namespace Bookstore.ConcurrencyLab.MemoryModel;

// volatile gives ordering, not atomicity: ++ is still read, add, write
public sealed class VolatileSoldCounter
{
    private volatile int _sold;

    public int Sold => _sold;

    public void RecordSale() => _sold++;
}
