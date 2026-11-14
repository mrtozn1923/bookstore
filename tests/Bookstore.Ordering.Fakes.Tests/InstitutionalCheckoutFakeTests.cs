using Bookstore.Inventory;
using Bookstore.Ordering.Institutional;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookstore.Ordering.Fakes.Tests;

public sealed class InstitutionalCheckoutFakeTests
{
    private const string Dune = "9780441172719";
    private static readonly CustomerAccount School = new("library@example.org", IsInstitutional: true, CompletedOrders: 3);

    private readonly IOrderRepository orders = A.Fake<IOrderRepository>();
    private readonly IStockService stock = A.Fake<IStockService>();
    private readonly IPriceRules prices = A.Fake<IPriceRules>();
    private readonly ICheckoutSession session = A.Fake<ICheckoutSession>();
    private readonly TimeProvider clock = A.Fake<TimeProvider>();

    public InstitutionalCheckoutFakeTests()
    {
        int available;
        A.CallTo(() => stock.TryReserve(A<string>._, A<int>._, out available))
            .Returns(true)
            .AssignsOutAndRefParameters(500);
        A.CallTo(() => prices.DiscountRate(A<CustomerAccount>.That.Matches(c => c.IsInstitutional), A<int>._))
            .Returns(0.10m);
        A.CallTo(() => clock.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 14, 9, 30, 0, TimeSpan.Zero));
        A.CallTo(() => orders.NextSequence(2026)).ReturnsNextFromSequence(42, 43);
    }

    private Task<CheckoutReceipt> Place(params OrderLine[] lines) =>
        new InstitutionalCheckout(orders, stock, prices, session, clock, NullLogger<InstitutionalCheckout>.Instance)
            .PlaceAsync(School, lines, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Bulk_order_is_saved_once_with_the_discount()
    {
        await Place(new OrderLine(Dune, 20, 12.50m));

        A.CallTo(() => orders.SaveAsync(A<Order>.That.Matches(o => o.Total == 225.00m), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        Assert.Equal("BS-2026-000042", session.LastOrderNumber);
    }

    [Fact]
    public async Task Short_stock_is_reported_with_the_available_count()
    {
        int available;
        A.CallTo(() => stock.TryReserve(Dune, 25, out available))
            .Returns(false)
            .AssignsOutAndRefParameters(7);

        InsufficientStockException ex = await Assert.ThrowsAsync<InsufficientStockException>(
            () => Place(new OrderLine(Dune, 25, 12.50m)));

        Assert.Equal(7, ex.Available);
        A.CallTo(() => orders.SaveAsync(A<Order>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Save_timeout_propagates()
    {
        A.CallTo(() => orders.SaveAsync(A<Order>._, A<CancellationToken>._))
            .ThrowsAsync(new TimeoutException("Database did not respond in 30s."));

        await Assert.ThrowsAsync<TimeoutException>(() => Place(new OrderLine(Dune, 20, 12.50m)));
    }
}
