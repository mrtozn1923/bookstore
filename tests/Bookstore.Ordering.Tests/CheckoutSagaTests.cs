using Bookstore.Contracts.Inventory;
using Bookstore.Contracts.Payments;
using Bookstore.Ordering.Checkout;
using Bookstore.Ordering.Domain;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OrderCancelled = Bookstore.Contracts.Ordering.OrderCancelled;
using OrderConfirmed = Bookstore.Contracts.Ordering.OrderConfirmed;

namespace Bookstore.Ordering.Tests;

// The saga's steps are plain methods: each test feeds one message and checks the state and the messages that come out.
public sealed class CheckoutSagaTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));
    private static readonly IMeterFactory MeterFactory = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
    private static readonly CheckoutMetrics Metrics = new(MeterFactory);
    private static readonly IOptions<CheckoutOptions> Options = Microsoft.Extensions.Options.Options.Create(new CheckoutOptions());
    private readonly InMemoryOrders orders = new();
    private readonly Order order;
    private readonly CheckoutSaga saga;

    public CheckoutSagaTests()
    {
        order = Order.Place(new Buyer("ada-subject", "Ada Lovelace", "ada@example.com"),
            [new OrderLine("9780141439587", "Emma", new Money(249.90m, "TRY"), 2)], Clock);
        orders.Add(order);
        (saga, _) = CheckoutSaga.Start(new StartCheckout(
            order.Id.Value, [new StockLine("9780141439587", 2)], order.Total.Amount, "TRY", "ada-subject", "ada@example.com"));
    }

    private Guid OrderId => order.Id.Value;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Start_reserves_the_stock()
    {
        (CheckoutSaga started, ReserveStock reserve) = CheckoutSaga.Start(new StartCheckout(
            OrderId, [new StockLine("9780141439587", 2)], 499.80m, "TRY", "ada-subject", "ada@example.com"));

        Assert.Equal(CheckoutStep.AwaitingStock, started.Step);
        Assert.Equal(OrderId, reserve.OrderId);
        Assert.Equal(2, Assert.Single(reserve.Lines).Quantity);
    }

    [Fact]
    public async Task Happy_path_ends_with_a_confirmed_order()
    {
        (AuthorizePayment authorize, CheckoutTimedOut timeout) = await saga.Handle(
            new StockReserved(OrderId, Guid.CreateVersion7(), Clock.GetUtcNow().AddMinutes(20)), orders, Options, Ct);
        Assert.Equal(499.80m, authorize.Amount);
        Assert.Equal(TimeSpan.FromMinutes(15), timeout.After);

        CommitStock commit = await saga.Handle(new PaymentSucceeded(OrderId, "pay_1"), orders, Ct);
        Assert.Equal(OrderId, commit.OrderId);
        Assert.Equal(OrderStatus.Paid, order.Status);

        OrderConfirmed confirmed = await saga.Handle(new StockCommitted(OrderId), orders, Clock, Metrics, Ct);
        Assert.Equal(OrderId, confirmed.OrderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.True(saga.IsCompleted());
    }

    [Fact]
    public async Task A_declined_payment_releases_the_stock_and_cancels_the_order()
    {
        await saga.Handle(new StockReserved(OrderId, Guid.CreateVersion7(), Clock.GetUtcNow().AddMinutes(20)), orders, Options, Ct);

        (ReleaseStock release, OrderCancelled cancelled) = await saga.Handle(new PaymentFailed(OrderId, "card-declined"), orders, Clock, Metrics, Ct);

        Assert.Equal("payment-failed", release.Reason);
        Assert.Equal("Payment failed: card-declined", cancelled.Reason);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.True(saga.IsCompleted());
    }

    [Fact]
    public async Task No_stock_cancels_without_compensation()
    {
        OrderCancelled cancelled = await saga.Handle(new StockReservationFailed(OrderId, ["9780141439587"]), orders, Clock, Metrics, Ct);

        Assert.Equal("Out of stock: 9780141439587", cancelled.Reason);
        Assert.True(saga.IsCompleted());
    }

    [Fact]
    public async Task A_timeout_while_waiting_for_payment_releases_the_stock()
    {
        await saga.Handle(new StockReserved(OrderId, Guid.CreateVersion7(), Clock.GetUtcNow().AddMinutes(20)), orders, Options, Ct);

        object[] messages = await saga.Handle(new CheckoutTimedOut(OrderId, TimeSpan.FromMinutes(15)), orders, Clock, Metrics, Ct);

        Assert.IsType<ReleaseStock>(messages[0]);
        Assert.IsType<OrderCancelled>(messages[1]);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task A_timeout_after_the_payment_does_nothing()
    {
        await saga.Handle(new StockReserved(OrderId, Guid.CreateVersion7(), Clock.GetUtcNow().AddMinutes(20)), orders, Options, Ct);
        await saga.Handle(new PaymentSucceeded(OrderId, "pay_1"), orders, Ct);

        object[] messages = await saga.Handle(new CheckoutTimedOut(OrderId, TimeSpan.FromMinutes(15)), orders, Clock, Metrics, Ct);

        Assert.Empty(messages);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.False(saga.IsCompleted());
    }

    [Fact]
    public async Task A_payment_for_a_cancelled_order_is_refunded()
    {
        order.Cancel("Payment did not arrive in time", Clock);

        RefundPayment? refund = await CheckoutSaga.NotFound(new PaymentSucceeded(OrderId, "pay_late"), orders, Ct);

        Assert.NotNull(refund);
        Assert.Equal(OrderId, refund.OrderId);
    }

    private sealed class InMemoryOrders : IOrderRepository
    {
        private readonly Dictionary<Guid, Order> items = [];

        public void Add(Order order) => items[order.Id.Value] = order;

        public Task<Order> GetAsync(Guid orderId, CancellationToken ct) => Task.FromResult(items[orderId]);
    }

    [Fact]
    public async Task A_declined_payment_records_how_long_checkout_took_and_why_it_ended()
    {
        using MetricCollector<double> durations = new(MeterFactory, CheckoutMetrics.MeterName, "bookstore.checkout.duration");
        await saga.Handle(new StockReserved(OrderId, Guid.CreateVersion7(), Clock.GetUtcNow().AddMinutes(20)), orders, Options, Ct);
        Clock.Advance(TimeSpan.FromSeconds(3));

        await saga.Handle(new PaymentFailed(OrderId, "card-declined"), orders, Clock, Metrics, Ct);

        CollectedMeasurement<double> measurement = Assert.Single(durations.GetMeasurementSnapshot());
        Assert.Equal(3, measurement.Value);
        Assert.True(measurement.MatchesTags(
            new KeyValuePair<string, object?>("bookstore.checkout.outcome", "cancelled"),
            new KeyValuePair<string, object?>("bookstore.checkout.reason", "payment-failed")));
    }
}
