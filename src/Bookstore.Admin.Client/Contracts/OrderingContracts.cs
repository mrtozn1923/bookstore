namespace Bookstore.Admin.Client.Contracts;

public sealed record OrderLineView(string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderView(
    int Id, string Status, DateTime PlacedAtUtc, IReadOnlyList<OrderLineView> Items, decimal Total, string Currency);
