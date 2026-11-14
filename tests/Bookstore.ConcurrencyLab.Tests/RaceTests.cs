using Bookstore.ConcurrencyLab.Races;

namespace Bookstore.ConcurrencyLab.Tests;

public class RaceTests
{
    [Fact]
    public void Atomic_increment_never_loses_a_sale()
    {
        SoldCounter counter = new();

        RaceRunner.RunTogether(8, _ =>
        {
            for (int i = 0; i < 100_000; i++)
                counter.RecordSaleAtomic();
        });

        Assert.Equal(800_000, counter.Sold);
    }

    [Fact]
    public void Locked_check_then_act_never_oversells()
    {
        for (int round = 0; round < 200; round++)
        {
            StockItem dune = new("9780441013593", 100);
            int reserved = 0;

            RaceRunner.RunTogether(8, _ =>
            {
                for (int i = 0; i < 50; i++)
                    if (dune.TryReserveLocked())
                        Interlocked.Increment(ref reserved);
            });

            Assert.Equal(100, reserved);
            Assert.Equal(0, dune.Available);
        }
    }

    [Fact]
    public void Compare_and_update_on_concurrent_dictionary_never_oversells()
    {
        for (int round = 0; round < 200; round++)
        {
            CatalogStock stock = new();
            stock.Set("9780441013593", 100);
            int reserved = 0;

            RaceRunner.RunTogether(8, _ =>
            {
                for (int i = 0; i < 50; i++)
                    if (stock.TryReserveAtomic("9780441013593"))
                        Interlocked.Increment(ref reserved);
            });

            Assert.Equal(100, reserved);
            Assert.Equal(0, stock.Get("9780441013593"));
        }
    }
}
