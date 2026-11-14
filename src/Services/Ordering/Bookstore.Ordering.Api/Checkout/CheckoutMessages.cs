using Bookstore.Contracts.Inventory;
using Wolverine;

namespace Bookstore.Ordering.Checkout;

// Local messages: they never leave Ordering, so they are not in Bookstore.Contracts.
public sealed record StartCheckout(Guid OrderId, IReadOnlyList<StockLine> Lines, decimal Total, string Currency, string CustomerEmail);

// Scheduled when stock is reserved. Wolverine stores it in orderingdb and delivers it after the delay.
public sealed record CheckoutTimedOut(Guid OrderId, TimeSpan After) : TimeoutMessage(After);

public sealed class CheckoutOptions
{
    // How long the customer has to pay once the stock is reserved.
    public TimeSpan PaymentTimeout { get; set; } = TimeSpan.FromMinutes(15);
}
