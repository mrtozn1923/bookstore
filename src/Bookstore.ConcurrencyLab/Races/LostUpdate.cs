namespace Bookstore.ConcurrencyLab.Races;

public sealed class SoldCounter
{
    private int _sold;

    public int Sold => _sold;

    // Data race: read, add and write are three separate steps
    public void RecordSale() => _sold++;

    public void RecordSaleAtomic() => Interlocked.Increment(ref _sold);
}

public static class RaceRunner
{
    // Starts all threads together so that their work really overlaps
    public static void RunTogether(int threads, Action<int> body)
    {
        using Barrier start = new(threads);
        Thread[] all = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            int id = t;
            all[t] = new Thread(() => { start.SignalAndWait(); body(id); });
            all[t].Start();
        }
        foreach (Thread thread in all)
            thread.Join();
    }
}
