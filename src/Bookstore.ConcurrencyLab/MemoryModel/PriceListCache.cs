using Bookstore.ConcurrencyLab.Hardware;

namespace Bookstore.ConcurrencyLab.MemoryModel;

public sealed class PriceList(decimal shippingFee)
{
    public decimal ShippingFee { get; } = shippingFee;
}

public interface IPriceListCache
{
    PriceList? Get();
}

// Broken: a separate bool says "loaded"; nothing orders the flag against the reference
public sealed class FlagCheckedCache : IPriceListCache
{
    private readonly Lock _gate = new();
    private PriceList? _list;
    private bool _loaded;

    public PriceList? Get()
    {
        if (!_loaded)
        {
            lock (_gate)
            {
                if (!_loaded)
                {
                    _list = new PriceList(29.90m);
                    _loaded = true;
                }
            }
        }
        return _list;
    }
}

// Correct under the .NET memory model: the reference itself is the check, read once into a local
public sealed class ReferenceCheckedCache : IPriceListCache
{
    private readonly Lock _gate = new();
    private PriceList? _list;

    public PriceList? Get()
    {
        PriceList? list = _list;
        if (list is null)
        {
            lock (_gate)
            {
                list = _list;
                if (list is null)
                {
                    list = new PriceList(29.90m);
                    _list = list;
                }
            }
        }
        return list;
    }
}

public sealed class LazyCache : IPriceListCache
{
    private readonly Lazy<PriceList> _list = new(() => new PriceList(29.90m));

    public PriceList? Get() => _list.Value;
}

// Both threads ask a fresh cache for the price list at the same moment; 1 = good list, 0 = null or default fee
public sealed class DoubleCheckTest(string name, Func<IPriceListCache> create) : LitmusTest
{
    private IPriceListCache _cache = create();
    private readonly Random _jitter = new(42);

    public override string Name => name;

    public override void Reset() => _cache = create();

    public override int Thread0() => Check(_cache.Get());

    public override int Thread1()
    {
        Thread.SpinWait(_jitter.Next(0, 20)); // arrive at a random moment while thread 0 may be initializing
        return Check(_cache.Get());
    }

    private static int Check(PriceList? list) => list is not null && list.ShippingFee == 29.90m ? 1 : 0;
}

// Thread 0 publishes a new object through a plain field; thread 1 reads it: 0 = not yet, 1 = complete, 2 = fee still default
public sealed class PublicationTest : LitmusTest
{
    private PriceList? _published;

    public override string Name => "publish via plain field";

    public override void Reset() => _published = null;

    public override int Thread0()
    {
        _published = new PriceList(29.90m);
        return 0;
    }

    public override int Thread1()
    {
        PriceList? list = _published;
        if (list is null)
            return 0;
        return list.ShippingFee == 29.90m ? 1 : 2;
    }
}
