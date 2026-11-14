using Bookstore.Inventory;
using static Bookstore.Ordering.Tests.Books;

namespace Bookstore.Ordering.Tests;

public sealed class StockReservationTests
{
    private readonly InMemoryStockLedger stock = new InMemoryStockLedger()
        .Receive(Dune, 1)
        .Receive(Hobbit, 5);

    [Fact]
    public async Task Placing_more_than_available_throws_insufficient_stock()
    {
        OrderPlacementService service = new(stock);

        InsufficientStockException ex = await Assert.ThrowsAsync<InsufficientStockException>(
            () => service.PlaceAsync(DuneAndHobbit(), TestContext.Current.CancellationToken));

        Assert.Equal(Dune, ex.Isbn);
        Assert.Equal(2, ex.Requested);
        Assert.Equal(1, ex.Available);
    }

    [Fact]
    public async Task Insufficient_stock_is_an_invalid_operation()
    {
        OrderPlacementService service = new(stock);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => service.PlaceAsync(DuneAndHobbit(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Zero_quantity_is_rejected_with_the_parameter_name()
    {
        Cart cart = new();

        Assert.Throws<ArgumentOutOfRangeException>("quantity", () => cart.Add(Dune, "Dune", 12.50m, quantity: 0));
    }

    [Fact]
    public void Placing_twice_names_the_current_status()
    {
        Order order = DuneAndHobbit();
        order.MarkPlaced();

        Assert.Throws<InvalidOperationException>(
            () => order.MarkPlaced(),
            ex => ex.Message.Contains("already Placed") ? null : $"Message does not name the status: {ex.Message}");
    }
}
