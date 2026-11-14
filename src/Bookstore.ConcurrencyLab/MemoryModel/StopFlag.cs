namespace Bookstore.ConcurrencyLab.MemoryModel;

// The shipping worker polls a stop flag; how the flag is read decides whether the loop ever ends
public sealed class PollingWorker
{
    private bool _stop;
    private volatile bool _stopVolatileField;

    public long Spins { get; private set; }

    public void RunPlain()
    {
        long spins = 0;
        while (!_stop)
            spins++;
        Spins = spins;
    }

    public void RunVolatileRead()
    {
        long spins = 0;
        while (!Volatile.Read(ref _stop))
            spins++;
        Spins = spins;
    }

    public void RunVolatileField()
    {
        long spins = 0;
        while (!_stopVolatileField)
            spins++;
        Spins = spins;
    }

    public void Stop()
    {
        _stop = true;
        _stopVolatileField = true;
    }
}

public static class StopFlagCheck
{
    // Starts the loop, asks it to stop after `stopAfter`, then waits at most `patience` for it to end
    public static (bool Ended, long Spins) Run(Action<PollingWorker> loop, TimeSpan stopAfter, TimeSpan patience)
    {
        PollingWorker worker = new();
        Thread thread = new(() => loop(worker)) { IsBackground = true };
        thread.Start();
        Thread.Sleep(stopAfter);
        worker.Stop();
        bool ended = thread.Join(patience);
        return (ended, worker.Spins);
    }
}
