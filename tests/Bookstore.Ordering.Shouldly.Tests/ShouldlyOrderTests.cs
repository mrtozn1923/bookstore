using Bookstore.Catalog;
using Bookstore.Inventory;

namespace Bookstore.Ordering.Shouldly.Tests;

public sealed class ShouldlyOrderTests
{
    private const string Dune = "9780441172719";
    private const string Hobbit = "9780261103344";

    private static Order NewOrder() => new(
        Guid.Parse("8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11"), "BS-2026-000123", "ada@example.com",
        [new OrderLine(Dune, 2, 12.50m), new OrderLine(Hobbit, 1, 9.90m)]);

    [Fact]
    public void Order_total_and_lines()
    {
        Order order = NewOrder();

        order.Total.ShouldBe(34.90m);
        order.Lines.Count.ShouldBe(2);
        order.Lines.ShouldContain(l => l.Isbn == Dune && l.Quantity == 2);
        order.Lines.ShouldAllBe(l => l.UnitPrice > 0m);
        order.CustomerEmail.ShouldEndWith("@example.com");
    }

    [Fact]
    public void Order_lines_match_regardless_of_order()
    {
        Order order = NewOrder();

        order.Lines.ShouldBe(
            [new OrderLine(Hobbit, 1, 9.90m), new OrderLine(Dune, 2, 12.50m)],
            ignoreOrder: true);
    }

    [Fact]
    public void Placed_order_satisfies_all_conditions()
    {
        Order order = NewOrder();
        order.MarkPlaced();

        order.ShouldSatisfyAllConditions(
            () => order.Status.ShouldBe(OrderStatus.Placed),
            () => order.Total.ShouldBe(34.90m),
            () => order.Lines.ShouldNotBeEmpty());
    }

    [Fact]
    public void Placing_twice_is_rejected()
    {
        Order order = NewOrder();
        order.MarkPlaced();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => order.MarkPlaced());

        ex.Message.ShouldContain("already Placed");
    }

    [Fact]
    public async Task Insufficient_stock_names_the_book()
    {
        OrderPlacementService service = new(new InMemoryStockLedger().Receive(Dune, 1));

        InsufficientStockException ex = await Should.ThrowAsync<InsufficientStockException>(
            () => service.PlaceAsync(NewOrder(), TestContext.Current.CancellationToken));

        ex.Available.ShouldBe(1);
    }

    [Fact]
    public void Campaign_ends_one_week_after_it_starts()
    {
        Campaign campaign = Campaign.StartNow("AUTUMN26", 0.15m, TimeSpan.FromDays(7), TimeProvider.System);

        campaign.EndsAtUtc.ShouldBe(DateTimeOffset.UtcNow.AddDays(7), tolerance: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Dto_is_equivalent_to_the_expected_shape()
    {
        OrderDto dto = new("BS-2026-000123", "ada@example.com", OrderStatus.Placed,
            [new OrderLineDto(Dune, "Dune", 2, 12.50m)], 25.00m);

        dto.ShouldBeEquivalentTo(new OrderDto("BS-2026-000123", "ada@example.com", OrderStatus.Placed,
            [new OrderLineDto(Dune, "Dune", 2, 12.50m)], 25.00m));
    }
}
