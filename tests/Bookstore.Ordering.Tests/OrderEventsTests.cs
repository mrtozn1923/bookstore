using System.ComponentModel;
using Bookstore.Inventory;
using static Bookstore.Ordering.Tests.Books;

namespace Bookstore.Ordering.Tests;

public sealed class OrderEventsTests
{
    [Fact]
    public void Marking_placed_raises_order_placed_with_the_total()
    {
        Order order = DuneAndHobbit();

        Assert.RaisedEvent<OrderPlacedEventArgs> raised = Assert.Raises<OrderPlacedEventArgs>(
            h => order.Placed += h,
            h => order.Placed -= h,
            () => order.MarkPlaced());

        Assert.Same(order, raised.Sender);
        Assert.Equal(new OrderPlacedEventArgs(order.Id, "BS-2026-000123", 34.90m), raised.Arguments);
    }

    [Fact]
    public void Marking_placed_notifies_status_change()
    {
        Order order = DuneAndHobbit();

        Assert.PropertyChanged(order, nameof(Order.Status), order.MarkPlaced);
    }

    [Fact]
    public async Task Successful_placement_raises_order_placed()
    {
        Order order = DuneAndHobbit();
        OrderPlacementService service = new(new InMemoryStockLedger().Receive(Dune, 5).Receive(Hobbit, 5));

        await Assert.RaisesAsync<OrderPlacedEventArgs>(
            h => order.Placed += h,
            h => order.Placed -= h,
            () => service.PlaceAsync(order, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Failed_reservation_raises_no_order_placed()
    {
        Order order = DuneAndHobbit();
        OrderPlacementService service = new(new InMemoryStockLedger());

        await Assert.NotRaisedAnyAsync<OrderPlacedEventArgs>(
            h => order.Placed += h,
            h => order.Placed -= h,
            () => Assert.ThrowsAsync<InsufficientStockException>(
                () => service.PlaceAsync(order, TestContext.Current.CancellationToken)));
        Assert.Equal(OrderStatus.Draft, order.Status);
    }
}
