namespace Bookstore.ConcurrencyLab.Exclusion;

public interface ITwoThreadLock
{
    void Enter(int me);
    void Exit(int me);
}

// Attempt 1: check the other's flag, then raise our own (the check and the write are separate)
public sealed class FlagOnlyLock : ITwoThreadLock
{
    private readonly int[] _wants = new int[2];

    public void Enter(int me)
    {
        while (Volatile.Read(ref _wants[1 - me]) == 1) { }
        Volatile.Write(ref _wants[me], 1);
    }

    public void Exit(int me) => Volatile.Write(ref _wants[me], 0);
}

// Peterson (1981): raise the flag, give the turn away, wait while the other wants in and it is its turn
public sealed class PetersonLock(bool fullFence) : ITwoThreadLock
{
    private readonly int[] _wants = new int[2];
    private int _turn;

    public void Enter(int me)
    {
        int other = 1 - me;
        Volatile.Write(ref _wants[me], 1);
        Volatile.Write(ref _turn, other);
        if (fullFence)
            Interlocked.MemoryBarrier(); // the store-load order the algorithm assumes
        while (Volatile.Read(ref _wants[other]) == 1 && Volatile.Read(ref _turn) == other) { }
    }

    public void Exit(int me) => Volatile.Write(ref _wants[me], 0);
}

public static class MutualExclusionCheck
{
    // Each thread adds `rounds` times inside the critical section; a lost update proves both were inside
    public static int LostUpdates(ITwoThreadLock gate, int rounds)
    {
        int counter = 0;
        using Barrier start = new(2);
        Thread[] threads = new Thread[2];
        for (int t = 0; t < 2; t++)
        {
            int me = t;
            threads[t] = new Thread(() =>
            {
                start.SignalAndWait();
                for (int i = 0; i < rounds; i++)
                {
                    gate.Enter(me);
                    counter++;
                    gate.Exit(me);
                }
            });
            threads[t].Start();
        }
        foreach (Thread thread in threads)
            thread.Join();
        return 2 * rounds - counter;
    }
}
