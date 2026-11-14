using Bookstore.ConcurrencyLab.Models;

namespace Bookstore.ConcurrencyLab.Tests;

public class ModelTests
{
    [Theory]
    [InlineData("lock")]
    [InlineData("actor")]
    [InlineData("csp")]
    public async Task Every_model_sells_exactly_the_stock(string model)
    {
        IOrderDesk desk = model switch
        {
            "lock" => new LockedOrderDesk(300),
            "actor" => new InventoryActor(300),
            _ => new OrderPipeline(300),
        };

        int accepted = await ModelRun.AcceptedAsync(desk, clients: 8, ordersPerClient: 100);

        Assert.Equal(300, accepted);
    }

    [Fact]
    public async Task Pipeline_rejects_invalid_quantities_before_touching_stock()
    {
        OrderPipeline pipeline = new(10);

        Assert.False(await pipeline.ReserveAsync(0));
        Assert.False(await pipeline.ReserveAsync(21));
        Assert.True(await pipeline.ReserveAsync(10));
        Assert.False(await pipeline.ReserveAsync(1));
    }
}
