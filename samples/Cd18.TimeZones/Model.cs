namespace Bookstore.Ordering.Data;

public class Order
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }        // instant: datetimeoffset in SQL Server
    public DateOnly InvoiceDate { get; set; }            // local business date: date
    public DateOnly RequestedDeliveryDate { get; set; }
}
