using Bookstore.ConcurrencyLab.Hardware;

namespace Bookstore.ConcurrencyLab.Tests;

// Only outcomes that are forbidden on every platform are asserted; the weak outcomes depend on the CPU
public class HardwareTests
{
    [Fact]
    public void Full_fence_forbids_both_threads_reading_zero()
    {
        long[,] outcomes = new LitmusRunner(new StoreBufferingFenced()).Run(200_000);

        Assert.Equal(0, outcomes[0, 0]);
    }

    [Fact]
    public void Release_acquire_flag_never_shows_ready_without_total()
    {
        long[,] outcomes = new LitmusRunner(new MessagePassingVolatile()).Run(200_000);

        Assert.Equal(0, outcomes[0, 2]); // ready=1, total=0
    }

    [Fact]
    public void One_location_is_seen_in_a_single_order()
    {
        (long reads, long wentBack) = new CoherenceCheck().Run(writes: 2_000_000, readers: 2);

        Assert.True(reads > 0);
        Assert.Equal(0, wentBack);
    }
}
