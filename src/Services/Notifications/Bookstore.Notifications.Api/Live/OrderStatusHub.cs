using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bookstore.Notifications.Live;

// What the browser receives. Only a hint that something changed: the UI reads the order itself from Ordering.
public sealed record OrderStatusChanged(Guid OrderId, string Status, string? Reason);

public interface IOrderStatusClient
{
    Task OrderStatusChanged(OrderStatusChanged change);
}

// The hub has no methods the client can call: the server only pushes. Who the user is comes from the
// access token the BFF forwards, never from the client.
[Authorize]
public sealed class OrderStatusHub : Hub<IOrderStatusClient>;

// SignalR looks for ClaimTypes.NameIdentifier by default; with MapInboundClaims = false Keycloak's subject is "sub".
// The same value travels in the events as CustomerId (post 10), so Clients.User(customerId) finds the connections.
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst("sub")?.Value;
}
