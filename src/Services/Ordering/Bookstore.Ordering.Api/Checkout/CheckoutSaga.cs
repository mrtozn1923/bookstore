using Bookstore.Contracts.Inventory;
using Bookstore.Contracts.Ordering;
using Bookstore.Contracts.Payments;
using Bookstore.Ordering.Domain;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.Persistence.Sagas;
using OrderCancelled = Bookstore.Contracts.Ordering.OrderCancelled;

namespace Bookstore.Ordering.Checkout;

public enum CheckoutStep
{
    AwaitingStock,
    AwaitingPayment,
    AwaitingStockCommit
}

// Orchestrates checkout: Ordering owns the workflow and tells Inventory and Payments what to do.
// The state lives in orderingdb (CheckoutSagas table) and is deleted when the saga completes.
public sealed class CheckoutSaga : Saga
{
    public Guid Id { get; set; }
    public CheckoutStep Step { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "";
    public string CustomerEmail { get; set; } = "";

    public static (CheckoutSaga, ReserveStock) Start(StartCheckout command) =>
    (
        new CheckoutSaga
        {
            Id = command.OrderId,
            Step = CheckoutStep.AwaitingStock,
            Total = command.Total,
            Currency = command.Currency,
            CustomerEmail = command.CustomerEmail
        },
        new ReserveStock(command.OrderId, command.Lines)
    );

    public async Task<(AuthorizePayment, CheckoutTimedOut)> Handle(
        [SagaIdentityFrom(nameof(StockReserved.OrderId))] StockReserved message,
        IOrderRepository orders, IOptions<CheckoutOptions> options, CancellationToken ct)
    {
        Order order = await orders.GetAsync(Id, ct);
        order.MarkStockReserved(message.ReservationId, message.ExpiresAtUtc.UtcDateTime);
        Step = CheckoutStep.AwaitingPayment;

        // Payment is requested and, in the same transaction, a timeout is scheduled for it.
        return (new AuthorizePayment(Id, Total, Currency, CustomerEmail), new CheckoutTimedOut(Id, options.Value.PaymentTimeout));
    }

    public async Task<OrderCancelled> Handle(
        [SagaIdentityFrom(nameof(StockReservationFailed.OrderId))] StockReservationFailed message,
        IOrderRepository orders, TimeProvider clock, CancellationToken ct)
    {
        // Nothing was reserved, so there is nothing to compensate: only the order is cancelled.
        return await CancelAsync(orders, $"Out of stock: {string.Join(", ", message.UnavailableIsbns)}", clock, ct);
    }

    public async Task<CommitStock> Handle(
        [SagaIdentityFrom(nameof(PaymentSucceeded.OrderId))] PaymentSucceeded message, IOrderRepository orders, CancellationToken ct)
    {
        Order order = await orders.GetAsync(Id, ct);
        order.MarkPaid(message.PaymentReference);
        Step = CheckoutStep.AwaitingStockCommit;
        return new CommitStock(Id);
    }

    public async Task<OrderConfirmed> Handle(
        [SagaIdentityFrom(nameof(StockCommitted.OrderId))] StockCommitted message, IOrderRepository orders,
        TimeProvider clock, CancellationToken ct)
    {
        Order order = await orders.GetAsync(Id, ct);
        order.Confirm();
        MarkCompleted();
        return new OrderConfirmed(Id, CustomerEmail, clock.GetUtcNow());
    }

    public async Task<(ReleaseStock, OrderCancelled)> Handle(
        [SagaIdentityFrom(nameof(PaymentFailed.OrderId))] PaymentFailed message, IOrderRepository orders,
        TimeProvider clock, CancellationToken ct)
    {
        // Compensation: the stock reserved in an earlier step goes back on sale.
        return (new ReleaseStock(Id, "payment-failed"), await CancelAsync(orders, $"Payment failed: {message.Reason}", clock, ct));
    }

    public async Task<object[]> Handle(
        [SagaIdentityFrom(nameof(CheckoutTimedOut.OrderId))] CheckoutTimedOut message, IOrderRepository orders,
        TimeProvider clock, CancellationToken ct)
    {
        // The payment arrived in time and the saga moved on: the timeout has nothing to do.
        if (Step != CheckoutStep.AwaitingPayment)
        {
            return [];
        }
        return [new ReleaseStock(Id, "payment-timeout"), await CancelAsync(orders, "Payment did not arrive in time", clock, ct)];
    }

    // The saga is gone (completed or timed out). A payment that succeeds now belongs to a cancelled order:
    // the money has to go back. That is a new operation (a refund), not an undo.
    public static async Task<RefundPayment?> NotFound(
        [SagaIdentityFrom(nameof(PaymentSucceeded.OrderId))] PaymentSucceeded message, IOrderRepository orders, CancellationToken ct)
    {
        Order order = await orders.GetAsync(message.OrderId, ct);
        return order.Status == OrderStatus.Cancelled ? new RefundPayment(message.OrderId, "order-cancelled") : null;
    }

    private async Task<OrderCancelled> CancelAsync(IOrderRepository orders, string reason, TimeProvider clock, CancellationToken ct)
    {
        Order order = await orders.GetAsync(Id, ct);
        order.Cancel(reason, clock);
        MarkCompleted();
        return new OrderCancelled(Id, CustomerEmail, reason, clock.GetUtcNow());
    }
}
