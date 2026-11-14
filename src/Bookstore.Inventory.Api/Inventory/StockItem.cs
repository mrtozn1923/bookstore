namespace Bookstore.Inventory;

public sealed record StockItem(string Isbn, string WarehouseId, int OnHand, int Reserved)
{
    public int Available => OnHand - Reserved;
}

public sealed record StockLevel(string Isbn, int Available, IReadOnlyList<WarehouseStock> Warehouses);

public sealed record WarehouseStock(string WarehouseId, string City, int Available);
