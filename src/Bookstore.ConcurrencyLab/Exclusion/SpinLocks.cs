namespace Bookstore.ConcurrencyLab.Exclusion;

// Test-and-set built on an atomic exchange
public sealed class TasLock
{
    private int _held;

    public void Enter()
    {
        while (Interlocked.Exchange(ref _held, 1) == 1)
        {
            // test-and-test-and-set: wait with plain reads, retry the atomic op only when it looks free
            while (Volatile.Read(ref _held) == 1)
                Thread.SpinWait(20);
        }
    }

    public void Exit() => Volatile.Write(ref _held, 0);
}
