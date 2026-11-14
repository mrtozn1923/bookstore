namespace Bookstore.Ordering;

// Queues named after their owner: they belong to Ordering, not to the services that publish into them.
public static class Queues
{
    public const string CatalogEvents = "ordering.catalog-events";
    public const string Checkout = "ordering.checkout";
}
