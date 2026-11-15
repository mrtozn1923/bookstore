namespace Bookstore.Admin.Components.Inventory;

// The inventory page's filter; a plain serializable class so it can be persisted with [PersistentState].
public sealed class InventoryFilter
{
    public int Threshold { get; set; } = 5;
    public bool OnlyLow { get; set; }
}
