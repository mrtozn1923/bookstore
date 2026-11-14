using System.Net;
using System.Net.Http.Json;
using Bookstore.Contracts.Inventory;
using Bookstore.Contracts.Payments;
using Bookstore.Ordering.Checkout;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Bookstore.Ordering.IntegrationTests;

public sealed class PlaceOrderTests(OrderingService ordering) : IClassFixture<OrderingService>
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // IMessageBus is scoped, like a request: a new one for every message the test sends
    private IMessageBus Bus() => ordering.Factory.Services.CreateScope().ServiceProvider.GetRequiredService<IMessageBus>();

    [Fact]
    public async Task An_accepted_order_publishes_OrderPlaced_and_asks_Inventory_for_stock()
    {
        HttpClient client = ordering.Factory.CreateClient();
        // The test plays Notifications (a queue on Ordering's exchange) and Inventory (its command queue)
        await using QueueProbe events = await QueueProbe.StartAsync(ordering.RabbitMqUri, "test.ordering-events", "bookstore.ordering");
        await using QueueProbe inventory = await QueueProbe.StartAsync(ordering.RabbitMqUri, "inventory.commands");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders",
            new { items = new[] { new { isbn = "9780141439587", quantity = 2, unitPrice = 249.90m } } }, Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        ReceivedMessage placed = await events.NextAsync("Bookstore.Contracts.Ordering.OrderPlaced", Wait);
        Assert.Contains($"\"customerId\":\"{TestUser.Subject}\"", placed.Body);
        Assert.Contains("\"total\":499.80", placed.Body);
        // The trace context travels with the message (post 12)
        Assert.StartsWith("00-", placed.Headers["parent-id"]);
        ReceivedMessage reserve = await inventory.NextAsync("Bookstore.Contracts.Inventory.ReserveStock", Wait);
        Assert.Contains("\"quantity\":2", reserve.Body);
    }

    [Fact]
    public async Task A_payment_racing_the_timeout_leaves_the_order_consistent()
    {
        HttpClient client = ordering.Factory.CreateClient();
        await using QueueProbe inventory = await QueueProbe.StartAsync(ordering.RabbitMqUri, "inventory.commands");
        await using QueueProbe payments = await QueueProbe.StartAsync(ordering.RabbitMqUri, "payments.commands");
        List<string> outcomes = [];

        for (int i = 0; i < 10; i++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders",
                new { items = new[] { new { isbn = "9780441172719", quantity = 1, unitPrice = 489m } } }, Ct);
            Guid orderId = Guid.Parse(response.Headers.Location!.Segments[^1]);
            await inventory.NextAsync("Bookstore.Contracts.Inventory.ReserveStock", Wait);
            await Bus().InvokeAsync(new StockReserved(orderId, Guid.CreateVersion7(), DateTimeOffset.UtcNow.AddMinutes(20)), Ct);

            // The payment and the timeout for the same saga, at the same moment, each with its own bus
            await Task.WhenAll(
                Bus().InvokeAsync(new PaymentSucceeded(orderId, $"pay_race_{i}"), Ct),
                Bus().InvokeAsync(new CheckoutTimedOut(orderId, TimeSpan.Zero), Ct));

            await using AsyncServiceScope scope = ordering.Factory.Services.CreateAsyncScope();
            OrderingDbContext db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            Order order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == new OrderId(orderId), Ct);
            CheckoutSaga? saga = await db.Set<CheckoutSaga>().AsNoTracking().SingleOrDefaultAsync(s => s.Id == orderId, Ct);

            // Only two consistent endings: paid and waiting for the stock commit, or cancelled with a refund on its way
            if (order.Status == OrderStatus.Paid)
            {
                Assert.Equal(CheckoutStep.AwaitingStockCommit, saga?.Step);
                outcomes.Add("payment first");
            }
            else
            {
                Assert.Equal(OrderStatus.Cancelled, order.Status);
                Assert.Null(saga);
                await payments.NextAsync("Bookstore.Contracts.Payments.RefundPayment", Wait);
                outcomes.Add("timeout first");
            }
        }

        // Wolverine logs every failed attempt; the retry policy from post 07 then runs the message again
        int conflicts = ordering.Logs.Lines.Count(l => l.Contains("SagaConcurrencyException"));
        TestContext.Current.TestOutputHelper?.WriteLine(ordering.Logs.Lines.FirstOrDefault(l => l.Contains("SagaConcurrencyException")) ?? "");
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"{string.Join(", ", outcomes.GroupBy(o => o).Select(g => $"{g.Key}: {g.Count()}"))}; saga version conflicts: {conflicts}");
    }
}
