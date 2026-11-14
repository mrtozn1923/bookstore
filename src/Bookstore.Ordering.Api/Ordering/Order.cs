namespace Bookstore.Ordering;

public sealed record Order(int Id, string CustomerId, string CustomerEmail, decimal Total, string Status);
