namespace Bookstore.Institutional.Domain;

public abstract record OrderEvent(int Id);

public sealed record OrderSubmitted(int Id, Money Total) : OrderEvent(Id);

public sealed record OrderApproved(int Id) : OrderEvent(Id);

public sealed record OrderRejected(int Id, string Reason) : OrderEvent(Id);

public sealed record OrderCancelled(int Id) : OrderEvent(Id);
