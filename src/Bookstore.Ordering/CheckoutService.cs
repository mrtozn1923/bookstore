namespace Bookstore.Ordering;

public sealed record CartLine(string Isbn, int Quantity, decimal UnitPrice);

public sealed record CheckoutCommand(CustomerType Customer, IReadOnlyList<CartLine> Lines, string? CouponCode);

public sealed record PlacedOrder(int OrderId, decimal Subtotal, decimal Discount, decimal Total);

public sealed record CheckoutResult(PlacedOrder? Order, string? Error);

public sealed class CheckoutService(IReadOnlyDictionary<string, Coupon> coupons)
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, PlacedOrder> _ordersByIdempotencyKey = [];
    private int _nextOrderId = 1001;

    public CheckoutResult PlaceOrder(CheckoutCommand command, string? idempotencyKey = null)
    {
        if (command.Lines.Count == 0)
        {
            return new CheckoutResult(null, "Cart is empty.");
        }

        if (command.Lines.Any(line => line.Quantity <= 0))
        {
            return new CheckoutResult(null, "Quantity must be positive.");
        }

        decimal subtotal = command.Lines.Sum(line => line.Quantity * line.UnitPrice);

        decimal discount = 0m;
        if (!string.IsNullOrWhiteSpace(command.CouponCode))
        {
            if (!coupons.TryGetValue(CouponCodes.Normalize(command.CouponCode), out Coupon? coupon))
            {
                return new CheckoutResult(null, $"Unknown coupon '{command.CouponCode}'.");
            }

            discount = CouponCalculator.Discount(coupon, subtotal, command.Customer);
        }

        lock (_lock)
        {
            // A second request with the same key doesn't open a new order; it returns the first one
            if (idempotencyKey is not null && _ordersByIdempotencyKey.TryGetValue(idempotencyKey, out PlacedOrder? existing))
            {
                return new CheckoutResult(existing, null);
            }

            PlacedOrder order = new(_nextOrderId++, subtotal, discount, subtotal - discount);
            if (idempotencyKey is not null)
            {
                _ordersByIdempotencyKey[idempotencyKey] = order;
            }

            return new CheckoutResult(order, null);
        }
    }
}
