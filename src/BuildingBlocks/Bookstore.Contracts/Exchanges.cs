namespace Bookstore.Contracts;

// One fanout exchange per publishing service. Consumers bind their own queues to it.
public static class Exchanges
{
    public const string Catalog = "bookstore.catalog";
    public const string Ordering = "bookstore.ordering";
}
