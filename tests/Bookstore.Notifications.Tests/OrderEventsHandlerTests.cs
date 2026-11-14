using Bookstore.Contracts.Ordering;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Live;
using Bookstore.Notifications.Orders;
using Microsoft.AspNetCore.SignalR;

namespace Bookstore.Notifications.Tests;

public sealed class OrderEventsHandlerTests
{
    private static readonly Guid OrderId = Guid.CreateVersion7();
    private readonly FakeHub hub = new();
    private readonly FakeEmails emails = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_cancelled_order_is_pushed_to_the_customer_with_the_reason()
    {
        await OrderEventsHandler.Handle(
            new OrderCancelled(OrderId, "sub-ada", "ada@example.com", "Payment failed: card_declined", DateTimeOffset.UtcNow), hub, emails, Ct);

        (string user, OrderStatusChanged change) = Assert.Single(hub.Pushes);
        Assert.Equal("sub-ada", user);
        Assert.Equal(new OrderStatusChanged(OrderId, "Cancelled", "Payment failed: card_declined"), change);
        Assert.Equal((OrderId, "cancelled", "ada@example.com"), Assert.Single(emails.Sent));
    }

    [Fact]
    public async Task A_confirmed_order_is_pushed_and_mailed()
    {
        await OrderEventsHandler.Handle(new OrderConfirmed(OrderId, "sub-grace", "grace@example.com", DateTimeOffset.UtcNow), hub, emails, Ct);

        Assert.Equal(("sub-grace", new OrderStatusChanged(OrderId, "Confirmed", null)), Assert.Single(hub.Pushes));
        Assert.Equal((OrderId, "confirmed", "grace@example.com"), Assert.Single(emails.Sent));
    }

    private sealed class FakeEmails : IOrderEmails
    {
        public List<(Guid, string, string)> Sent { get; } = [];

        public Task SendOnceAsync(Guid orderId, string kind, string to, string subject, string body, CancellationToken ct)
        {
            Sent.Add((orderId, kind, to));
            return Task.CompletedTask;
        }
    }

    // Records Clients.User(id).OrderStatusChanged(...) calls; nothing else of IHubContext is used by the handler.
    private sealed class FakeHub : IHubContext<OrderStatusHub, IOrderStatusClient>
    {
        public List<(string User, OrderStatusChanged Change)> Pushes { get; } = [];
        public IHubClients<IOrderStatusClient> Clients => new UserClients(this);
        public IGroupManager Groups => throw new NotSupportedException();

        private sealed class UserClients(FakeHub hub) : IHubClients<IOrderStatusClient>
        {
            public IOrderStatusClient User(string userId) => new Recorder(change => hub.Pushes.Add((userId, change)));
            public IOrderStatusClient All => throw new NotSupportedException();
            public IOrderStatusClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
            public IOrderStatusClient Client(string connectionId) => throw new NotSupportedException();
            public IOrderStatusClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
            public IOrderStatusClient Group(string groupName) => throw new NotSupportedException();
            public IOrderStatusClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
            public IOrderStatusClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
            public IOrderStatusClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
        }

        private sealed class Recorder(Action<OrderStatusChanged> record) : IOrderStatusClient
        {
            public Task OrderStatusChanged(OrderStatusChanged change)
            {
                record(change);
                return Task.CompletedTask;
            }
        }
    }
}
