namespace Bookstore.Contracts.Ordering;

/// <summary>Published when a customer's order has been placed and stock has been reserved.</summary>
/// <param name="OrderId">Ordering module's order id.</param>
/// <param name="CustomerEmail">Customer's e-mail address for notifications.</param>
/// <param name="PlacedAtUtc">When the order was placed, in UTC.</param>
/// <param name="Lines">Ordered books.</param>
/// <param name="Total">Order total.</param>
/// <param name="Currency">ISO 4217 currency code, for example TRY.</param>
public sealed record OrderPlaced(
    int OrderId,
    string CustomerEmail,
    DateTime PlacedAtUtc,
    IReadOnlyList<OrderPlacedLine> Lines,
    decimal Total,
    string Currency);

/// <summary>One ordered book inside <see cref="OrderPlaced"/>.</summary>
/// <param name="Isbn">ISBN-13 of the book.</param>
/// <param name="Title">Book title at the time of the order.</param>
/// <param name="Quantity">Number of copies.</param>
/// <param name="UnitPrice">Unit price at the time of the order.</param>
public sealed record OrderPlacedLine(string Isbn, string Title, int Quantity, decimal UnitPrice);
