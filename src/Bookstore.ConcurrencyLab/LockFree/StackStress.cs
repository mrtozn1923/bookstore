namespace Bookstore.ConcurrencyLab.LockFree;

public static class StackStress
{
    // Every thread pushes unique ids and pops; afterwards every id must have come out exactly once
    public static (int Missing, int Duplicated) Run(IConcurrentStack<int> stack, int threads, int pushesPerThread)
    {
        int total = threads * pushesPerThread;
        int[] seen = new int[total];
        Races.RaceRunner.RunTogether(threads, t =>
        {
            for (int i = 0; i < pushesPerThread; i++)
            {
                stack.Push(t * pushesPerThread + i);
                if (stack.TryPop(out int id))
                    Interlocked.Increment(ref seen[id]);
            }
        });
        // drain what is left; a corrupted stack can contain cycles, so stop after `total` pops
        for (int i = 0; i < total && stack.TryPop(out int id); i++)
            seen[id]++;
        return (seen.Count(c => c == 0), seen.Count(c => c > 1));
    }
}
