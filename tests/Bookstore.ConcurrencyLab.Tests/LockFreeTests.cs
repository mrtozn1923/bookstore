using Bookstore.ConcurrencyLab.LockFree;
using Bookstore.ConcurrencyLab.Races;

namespace Bookstore.ConcurrencyLab.Tests;

public class LockFreeTests
{
    [Fact]
    public void Cas_loop_counter_counts_every_add()
    {
        LockFreeCounter counter = new();

        RaceRunner.RunTogether(4, _ =>
        {
            for (int i = 0; i < 250_000; i++)
                counter.Add(1);
        });

        Assert.Equal(1_000_000, counter.Value);
    }

    [Fact]
    public void Treiber_stack_returns_every_pushed_id_exactly_once()
    {
        (int missing, int duplicated) = StackStress.Run(new TreiberStack<int>(), threads: 4, pushesPerThread: 100_000);

        Assert.Equal(0, missing);
        Assert.Equal(0, duplicated);
    }

    [Fact]
    public void Treiber_stack_histories_are_linearizable()
    {
        for (int round = 0; round < 500; round++)
        {
            List<StackCall> history = StackHistory.Record(new TreiberStack<int>(), threads: 3, callsPerThread: 4);
            Assert.True(LinearizabilityChecker.IsLinearizable(history));
        }
    }

    [Fact]
    public void Pop_that_misses_a_completed_push_is_not_linearizable()
    {
        StackCall[] history =
        [
            new(0, IsPush: true, Value: 7, Popped: false, Invoked: 1, Returned: 2),
            new(1, IsPush: false, Value: 0, Popped: false, Invoked: 3, Returned: 4),
        ];

        Assert.False(LinearizabilityChecker.IsLinearizable(history));
    }

    [Fact]
    public void Overlapping_push_and_empty_pop_is_linearizable()
    {
        StackCall[] history =
        [
            new(0, IsPush: true, Value: 7, Popped: false, Invoked: 1, Returned: 4),
            new(1, IsPush: false, Value: 0, Popped: false, Invoked: 2, Returned: 3),
        ];

        Assert.True(LinearizabilityChecker.IsLinearizable(history));
    }
}
