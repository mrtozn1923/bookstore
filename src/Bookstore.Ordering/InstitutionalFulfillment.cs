namespace Bookstore.Ordering;

public enum FulfillmentState
{
    Paid,
    PartiallyShipped,
    Shipped,
    ClosedWithCancellation,
}

public sealed record InstitutionalOrderLine(string Isbn, int Quantity, decimal UnitPrice);

public sealed record Shipment(int Number, IReadOnlyDictionary<string, int> Items, decimal InvoiceAmount, decimal ShippingFee);

public sealed class InstitutionalFulfillment(
    IReadOnlyList<InstitutionalOrderLine> lines, decimal shippingFee, DateTimeOffset orderedAt, TimeProvider clock)
{
    public const int MaxShipments = 3;
    public static readonly TimeSpan BackorderWindow = TimeSpan.FromDays(30);

    private readonly Dictionary<string, int> _shipped = lines.ToDictionary(line => line.Isbn, _ => 0);
    private readonly List<Shipment> _shipments = [];

    public FulfillmentState State { get; private set; } = FulfillmentState.Paid;

    public IReadOnlyList<Shipment> Shipments => _shipments;

    public int Remaining(string isbn) => lines.Single(line => line.Isbn == isbn).Quantity - _shipped[isbn];

    public Shipment Ship(IReadOnlyDictionary<string, int> items)
    {
        if (State is not (FulfillmentState.Paid or FulfillmentState.PartiallyShipped))
        {
            throw new InvalidOperationException($"Cannot ship an order that is {State}.");
        }

        if (items.Count == 0)
        {
            throw new ArgumentException("A shipment must contain at least one book.", nameof(items));
        }

        foreach ((string isbn, int quantity) in items)
        {
            if (!_shipped.ContainsKey(isbn))
            {
                throw new ArgumentException($"{isbn} is not in this order.", nameof(items));
            }

            if (quantity <= 0 || quantity > Remaining(isbn))
            {
                throw new ArgumentException($"Quantity for {isbn} must be between 1 and {Remaining(isbn)}.", nameof(items));
            }
        }

        int number = _shipments.Count + 1;
        bool completesOrder = lines.All(line => _shipped[line.Isbn] + items.GetValueOrDefault(line.Isbn) == line.Quantity);
        if (number == MaxShipments && !completesOrder)
        {
            throw new InvalidOperationException($"Shipment {MaxShipments} must complete the order.");
        }

        foreach ((string isbn, int quantity) in items)
        {
            _shipped[isbn] += quantity;
        }

        decimal invoice = items.Sum(item => item.Value * lines.Single(line => line.Isbn == item.Key).UnitPrice);
        Shipment shipment = new(number, items, invoice, number == 1 ? shippingFee : 0m);
        _shipments.Add(shipment);
        State = completesOrder ? FulfillmentState.Shipped : FulfillmentState.PartiallyShipped;
        return shipment;
    }

    public decimal CancelRemaining()
    {
        if (State is not (FulfillmentState.Paid or FulfillmentState.PartiallyShipped))
        {
            throw new InvalidOperationException($"Cannot cancel the rest of an order that is {State}.");
        }

        if (clock.GetUtcNow() - orderedAt < BackorderWindow)
        {
            throw new InvalidOperationException("Remaining books can be cancelled only after 30 days.");
        }

        decimal refund = lines.Sum(line => (line.Quantity - _shipped[line.Isbn]) * line.UnitPrice);
        State = FulfillmentState.ClosedWithCancellation;
        return refund;
    }
}
