using AwesomeAssertions.Execution;
using Bookstore.Inventory;
using static Bookstore.Ordering.AwesomeAssertions.Tests.Records;

namespace Bookstore.Ordering.AwesomeAssertions.Tests;

public sealed class OrderPlacementTests
{
    private static Order NewOrder() => new(
        Guid.NewGuid(), "BS-2026-000123", "ada@example.com",
        [new OrderLine(Dune, 2, 12.50m), new OrderLine(Hobbit, 1, 9.90m)]);

    [Fact]
    public void Placing_twice_is_rejected()
    {
        Order order = NewOrder();
        order.MarkPlaced();

        Action act = () => order.MarkPlaced();

        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("Order BS-2026-000123 is already *");
    }

    [Fact]
    public async Task Insufficient_stock_names_the_book()
    {
        OrderPlacementService service = new(new InMemoryStockLedger().Receive(Dune, 1));

        Func<Task> act = () => service.PlaceAsync(NewOrder(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InsufficientStockException>()
            .Where(e => e.Isbn == Dune && e.Requested == 2 && e.Available == 1);
    }

    [Fact]
    public async Task Placed_order_has_status_and_total()
    {
        Order order = NewOrder();
        OrderPlacementService service = new(new InMemoryStockLedger().Receive(Dune, 5).Receive(Hobbit, 5));

        await service.PlaceAsync(order, TestContext.Current.CancellationToken);

        using (new AssertionScope())
        {
            order.Status.Should().Be(OrderStatus.Placed);
            order.Total.Should().Be(34.90m);
            order.Lines.Should().HaveCount(2);
        }
    }
}
