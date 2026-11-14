using Bookstore.Inventory;
using Bookstore.Ordering.Institutional;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookstore.Ordering.Legacy.Tests;

public sealed class InstitutionalCheckoutTests
{
    private const string Dune = "9780441172719";
    private const string Hobbit = "9780261103344";
    private static readonly CustomerAccount School = new("library@example.org", IsInstitutional: true, CompletedOrders: 3);

    private readonly Mock<IOrderRepository> orders = new();
    private readonly Mock<IStockService> stock = new();
    private readonly Mock<ICheckoutSession> session = new();
    private readonly Mock<TimeProvider> clock = new();
    private readonly IPriceRules prices = Mock.Of<IPriceRules>(
        p => p.DiscountRate(It.Is<CustomerAccount>(c => c.IsInstitutional), It.IsAny<int>()) == 0.10m);

    public InstitutionalCheckoutTests()
    {
        int available = 500;
        stock.Setup(s => s.TryReserve(It.IsRegex(@"^97[89]\d{10}$"), It.IsInRange(1, 500, Moq.Range.Inclusive), out available))
            .Returns(true);
        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 14, 9, 30, 0, TimeSpan.Zero));
        orders.Setup(o => o.NextSequence(2026)).Returns(42);
        session.SetupProperty(s => s.LastOrderNumber);
    }

    private InstitutionalCheckout CreateSut() => new(
        orders.Object, stock.Object, prices, session.Object, clock.Object, NullLogger<InstitutionalCheckout>.Instance);

    private Task<CheckoutReceipt> Place(params OrderLine[] lines) =>
        CreateSut().PlaceAsync(School, lines, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Bulk_order_is_saved_with_the_institutional_discount()
    {
        Order? saved = null;
        orders.Setup(o => o.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => saved = order)
            .Returns(Task.CompletedTask);

        CheckoutReceipt receipt = await Place(new OrderLine(Dune, 20, 12.50m), new OrderLine(Hobbit, 5, 9.90m));

        Assert.Equal(new CheckoutReceipt("BS-2026-000042", 269.55m, clock.Object.GetUtcNow()), receipt);
        Assert.NotNull(saved);
        Assert.Equal([new OrderLine(Dune, 20, 11.25m), new OrderLine(Hobbit, 5, 8.91m)], saved.Lines);
        orders.Verify(o => o.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Order_numbers_follow_the_repository_sequence()
    {
        orders.SetupSequence(o => o.NextSequence(2026))
            .Returns(42)
            .Returns(43);

        CheckoutReceipt first = await Place(new OrderLine(Dune, 20, 12.50m));
        CheckoutReceipt second = await Place(new OrderLine(Hobbit, 30, 9.90m));

        Assert.Equal("BS-2026-000042", first.OrderNumber);
        Assert.Equal("BS-2026-000043", second.OrderNumber);
    }

    [Fact]
    public async Task Short_stock_is_reported_with_the_available_count()
    {
        int available = 7;
        stock.Setup(s => s.TryReserve(Dune, 25, out available)).Returns(false);

        InsufficientStockException ex = await Assert.ThrowsAsync<InsufficientStockException>(
            () => Place(new OrderLine(Dune, 25, 12.50m)));

        Assert.Equal(7, ex.Available);
        orders.Verify(o => o.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Fewer_than_twenty_copies_reserve_nothing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Place(new OrderLine(Dune, 19, 12.50m)));

        stock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Save_timeout_propagates_and_session_is_not_updated()
    {
        orders.Setup(o => o.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database did not respond in 30s."));

        await Assert.ThrowsAsync<TimeoutException>(() => Place(new OrderLine(Dune, 20, 12.50m)));

        Assert.Null(session.Object.LastOrderNumber);
    }

    [Fact]
    public async Task Session_remembers_the_last_order_number()
    {
        await Place(new OrderLine(Dune, 20, 12.50m));

        Assert.Equal("BS-2026-000042", session.Object.LastOrderNumber);
    }

    [Fact]
    public async Task Unit_of_work_is_committed_after_the_save()
    {
        List<string> calls = [];
        orders.Setup(o => o.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save"))
            .Returns(Task.CompletedTask);
        orders.As<IUnitOfWork>()
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("commit"))
            .Returns(Task.CompletedTask);

        await Place(new OrderLine(Dune, 20, 12.50m));

        Assert.Equal(["save", "commit"], calls);
    }
}
