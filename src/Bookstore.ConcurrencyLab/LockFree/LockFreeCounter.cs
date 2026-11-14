namespace Bookstore.ConcurrencyLab.LockFree;

// The CAS loop: read, compute, publish only if nobody changed the value in between, otherwise retry
public sealed class LockFreeCounter
{
    private long _value;

    public long Value => Volatile.Read(ref _value);

    // Returns how many CAS attempts failed before ours succeeded (a measure of contention)
    public int Add(long amount)
    {
        int failed = 0;
        while (true)
        {
            long seen = Volatile.Read(ref _value);
            if (Interlocked.CompareExchange(ref _value, seen + amount, seen) == seen)
                return failed;
            failed++;
        }
    }
}
