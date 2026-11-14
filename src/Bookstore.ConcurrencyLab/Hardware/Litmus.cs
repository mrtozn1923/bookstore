using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bookstore.ConcurrencyLab.Hardware;

// Two shared ints, 128 bytes apart so they never share a cache line
[StructLayout(LayoutKind.Explicit, Size = 256)]
public struct Cells
{
    [FieldOffset(0)] public int X;
    [FieldOffset(128)] public int Y;
}

// A litmus test: two tiny threads, every round starts from X = Y = 0, each thread returns what it read
// NoInlining + AggressiveOptimization: every run measures the same optimized machine code
public abstract class LitmusTest
{
    protected Cells Shared;

    public abstract string Name { get; }

    public virtual void Reset() => Shared = default;

    public abstract int Thread0();

    public abstract int Thread1();
}

// Store buffering (SB): write your own cell, then read the other one
public sealed class StoreBufferingPlain : LitmusTest
{
    public override string Name => "SB plain";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread0()
    {
        Shared.X = 1;
        return Shared.Y;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread1()
    {
        Shared.Y = 1;
        return Shared.X;
    }
}

public sealed class StoreBufferingVolatile : LitmusTest
{
    public override string Name => "SB Volatile.Read/Write";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread0()
    {
        Volatile.Write(ref Shared.X, 1);
        return Volatile.Read(ref Shared.Y);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread1()
    {
        Volatile.Write(ref Shared.Y, 1);
        return Volatile.Read(ref Shared.X);
    }
}

public sealed class StoreBufferingFenced : LitmusTest
{
    public override string Name => "SB full fence";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread0()
    {
        Shared.X = 1;
        Interlocked.MemoryBarrier();
        return Shared.Y;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread1()
    {
        Shared.Y = 1;
        Interlocked.MemoryBarrier();
        return Shared.X;
    }
}

// Message passing (MP): X = order total, Y = "ready" flag. Thread 0 publishes, thread 1 reads flag then total
public sealed class MessagePassingPlain : LitmusTest
{
    public override string Name => "MP plain";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread0()
    {
        Shared.X = 1; // total
        Shared.Y = 1; // ready
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread1()
    {
        int ready = Shared.Y;
        int total = Shared.X;
        return ready * 2 + total;
    }
}

public sealed class MessagePassingVolatile : LitmusTest
{
    public override string Name => "MP Volatile.Read/Write";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread0()
    {
        Shared.X = 1;
        Volatile.Write(ref Shared.Y, 1);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public override int Thread1()
    {
        int ready = Volatile.Read(ref Shared.Y);
        int total = Shared.X;
        return ready * 2 + total;
    }
}

// Runs a litmus test round after round: a coordinator resets the cells and releases both threads at once
public sealed class LitmusRunner(LitmusTest test)
{
    private int _go;
    private int _done;
    private int _r0;
    private int _r1;

    // outcomes[r0, r1] for SB; for MP r1 is ready*2+total
    public long[,] Run(int rounds)
    {
        long[,] outcomes = new long[2, 4];
        Thread[] workers = [new Thread(() => Work(0, rounds)), new Thread(() => Work(1, rounds))];
        foreach (Thread worker in workers)
            worker.Start();

        for (int round = 1; round <= rounds; round++)
        {
            test.Reset();
            Volatile.Write(ref _done, 0);
            Volatile.Write(ref _go, round);
            while (Volatile.Read(ref _done) != 2) { }
            outcomes[_r0, _r1]++;
        }

        foreach (Thread worker in workers)
            worker.Join();
        return outcomes;
    }

    private void Work(int me, int rounds)
    {
        for (int round = 1; round <= rounds; round++)
        {
            while (Volatile.Read(ref _go) != round) { }
            if (me == 0)
                _r0 = test.Thread0();
            else
                _r1 = test.Thread1();
            Interlocked.Increment(ref _done);
        }
    }
}
