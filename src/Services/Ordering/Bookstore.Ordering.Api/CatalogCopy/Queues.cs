namespace Bookstore.Ordering.CatalogCopy;

public static class Queues
{
    // Named after the owner: the queue belongs to Ordering, not to Catalog.
    public const string CatalogEvents = "ordering.catalog-events";
}
