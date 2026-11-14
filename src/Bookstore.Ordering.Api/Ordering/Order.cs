namespace Bookstore.Ordering;

public sealed record Order(int Id, string CustomerId, string CustomerEmail, decimal Total, string Status);

public sealed record OrderLine(int OrderId, string Isbn, int Quantity);

public sealed record Purchase(string Isbn, bool PurchasedBefore, int? LastOrderId);
