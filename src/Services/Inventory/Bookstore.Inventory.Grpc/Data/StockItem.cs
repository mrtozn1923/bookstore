namespace Bookstore.Inventory.Data;

public sealed class StockItem
{
    public required string Isbn { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // SQL Server rowversion: changes on every UPDATE of the row, used as the concurrency token.
    public byte[] RowVersion { get; set; } = [];

    public int Available => QuantityOnHand - QuantityReserved;
}

public enum ReservationStatus
{
    Active,
    Released,
    Expired,
    Committed
}

public sealed class StockReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public List<ReservationLine> Lines { get; } = [];
}

public sealed class ReservationLine
{
    public required string Isbn { get; set; }
    public int Quantity { get; set; }
}
