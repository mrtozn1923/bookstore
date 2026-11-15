namespace Bookstore.Web.Catalog;

public sealed class CatalogOptions
{
    // Demo switch: makes every catalog call fail like a database that stopped answering.
    public bool SimulateOutage { get; set; }
}
