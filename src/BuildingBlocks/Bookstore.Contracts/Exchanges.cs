namespace Bookstore.Contracts;

// Events: one fanout exchange per publishing service. Consumers bind their own queues to it.
public static class Exchanges
{
    public const string Catalog = "bookstore.catalog";
    public const string Ordering = "bookstore.ordering";
    public const string Inventory = "bookstore.inventory";
    public const string Payments = "bookstore.payments";
}

// Commands: one queue per receiving service. The receiver owns it; senders only know its name.
public static class CommandQueues
{
    public const string Inventory = "inventory.commands";
    public const string Payments = "payments.commands";
}
