using System.Collections.Immutable;

namespace Bookstore.ConcurrencyLab.LockFree;

// One completed call: when it was invoked, when it returned, what it did
public sealed record StackCall(int Thread, bool IsPush, int Value, bool Popped, long Invoked, long Returned)
{
    public override string ToString() =>
        $"[{Invoked,2}..{Returned,2}] T{Thread} " + (IsPush ? $"Push({Value})" : Popped ? $"TryPop() -> {Value}" : "TryPop() -> empty");
}

public static class StackHistory
{
    // Runs `threads` threads, each alternating Push(unique id) and TryPop; a shared counter stamps invoke/return
    public static List<StackCall> Record(IConcurrentStack<int> stack, int threads, int callsPerThread)
    {
        long clock = 0;
        List<StackCall>[] perThread = [.. Enumerable.Range(0, threads).Select(_ => new List<StackCall>())];
        Races.RaceRunner.RunTogether(threads, t =>
        {
            for (int i = 0; i < callsPerThread; i++)
            {
                long invoked = Interlocked.Increment(ref clock);
                if (i % 2 == 0)
                {
                    int id = t * 100 + i;
                    stack.Push(id);
                    perThread[t].Add(new StackCall(t, true, id, false, invoked, Interlocked.Increment(ref clock)));
                }
                else
                {
                    bool popped = stack.TryPop(out int value);
                    perThread[t].Add(new StackCall(t, false, value, popped, invoked, Interlocked.Increment(ref clock)));
                }
            }
        });
        return [.. perThread.SelectMany(calls => calls).OrderBy(c => c.Invoked)];
    }
}

// Herlihy and Wing: a history is linearizable if each call can be given one instant between its invoke and return
// such that the calls, in instant order, are a correct run of an ordinary sequential stack. Brute force, small histories only.
public static class LinearizabilityChecker
{
    public static bool IsLinearizable(IReadOnlyList<StackCall> history) =>
        Search([.. history], ImmutableStack<int>.Empty);

    private static bool Search(ImmutableList<StackCall> pending, ImmutableStack<int> stack)
    {
        if (pending.IsEmpty)
            return true;
        // a call can go next only if no other pending call returned before it was invoked
        long firstReturn = pending.Min(c => c.Returned);
        foreach (StackCall call in pending.Where(c => c.Invoked < firstReturn))
        {
            ImmutableStack<int> next;
            if (call.IsPush)
                next = stack.Push(call.Value);
            else if (!call.Popped)
            {
                if (!stack.IsEmpty) continue;
                next = stack;
            }
            else
            {
                if (stack.IsEmpty || stack.Peek() != call.Value) continue;
                next = stack.Pop();
            }
            if (Search(pending.Remove(call), next))
                return true;
        }
        return false;
    }
}
