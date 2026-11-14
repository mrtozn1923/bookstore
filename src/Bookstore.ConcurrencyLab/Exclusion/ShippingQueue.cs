namespace Bookstore.ConcurrencyLab.Exclusion;

public sealed record Parcel(int OrderId);

// Bounded buffer as a monitor: one lock, two conditions (not full, not empty)
public sealed class ShippingQueue(int capacity, bool recheckWithWhile)
{
    private readonly Queue<Parcel> _parcels = new();
    private readonly object _monitor = new();

    public void Put(Parcel parcel)
    {
        lock (_monitor)
        {
            while (_parcels.Count == capacity)
                Monitor.Wait(_monitor);
            _parcels.Enqueue(parcel);
            Monitor.PulseAll(_monitor);
        }
    }

    public Parcel Take()
    {
        lock (_monitor)
        {
            if (recheckWithWhile)
            {
                while (_parcels.Count == 0)
                    Monitor.Wait(_monitor);
            }
            else if (_parcels.Count == 0)
            {
                Monitor.Wait(_monitor); // the condition may be false again when we wake up
            }
            Parcel parcel = _parcels.Dequeue();
            Monitor.PulseAll(_monitor);
            return parcel;
        }
    }
}
