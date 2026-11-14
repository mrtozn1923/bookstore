namespace Bookstore.ConcurrencyLab.Hardware;

// Coherence: one writer counts up on ONE location; no reader may ever see the value go back
public sealed class CoherenceCheck
{
    private int _soldToday;

    public (long Reads, long WentBack) Run(int writes, int readers)
    {
        long reads = 0, wentBack = 0;
        Thread writer = new(() =>
        {
            for (int i = 1; i <= writes; i++)
                _soldToday = i; // plain store
        });
        Thread[] watchers = [.. Enumerable.Range(0, readers).Select(_ => new Thread(() =>
        {
            long myReads = 0, myBack = 0;
            int last = 0;
            while (last < writes)
            {
                // Volatile only so the JIT cannot merge the reads into one; ordering on one address comes from coherence
                int now = Volatile.Read(ref _soldToday);
                if (now < last)
                    myBack++;
                last = now;
                myReads++;
            }
            Interlocked.Add(ref reads, myReads);
            Interlocked.Add(ref wentBack, myBack);
        }))];

        foreach (Thread t in watchers) t.Start();
        writer.Start();
        writer.Join();
        foreach (Thread t in watchers) t.Join();
        return (reads, wentBack);
    }
}
