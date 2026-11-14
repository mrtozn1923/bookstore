namespace Bookstore.ConcurrencyLab.Deadlocks;

public static class PoliteTransfers
{
    // Two threads each hold one lock and politely back off when the other is busy.
    // Returns how many times they had to give up and retry.
    public static long Retries(int transfers, bool randomBackoff)
    {
        object a = new(), b = new();
        long retries = 0;
        using Barrier start = new(2);
        void Worker(object mine, object theirs, int seed)
        {
            Random random = new(seed);
            start.SignalAndWait();
            for (int i = 0; i < transfers; i++)
            {
                while (true)
                {
                    lock (mine)
                    {
                        if (Monitor.TryEnter(theirs))
                        {
                            Monitor.Exit(theirs);
                            break;
                        }
                    }
                    Interlocked.Increment(ref retries);
                    if (randomBackoff)
                        Thread.SpinWait(random.Next(1, 200));
                }
            }
        }
        Thread t1 = new(() => Worker(a, b, 1));
        Thread t2 = new(() => Worker(b, a, 2));
        t1.Start(); t2.Start();
        t1.Join(); t2.Join();
        return retries;
    }
}
