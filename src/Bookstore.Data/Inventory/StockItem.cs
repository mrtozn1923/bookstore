namespace Bookstore.Inventory;

// Inventory knows a book only by its id; it never navigates into the Catalog module.
public sealed class StockItem
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }

    public int Available => QuantityOnHand - QuantityReserved;
}
