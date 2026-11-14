namespace Bookstore.Ordering;

public sealed record Order(int Id, string CustomerEmail, decimal Total, string Status);
