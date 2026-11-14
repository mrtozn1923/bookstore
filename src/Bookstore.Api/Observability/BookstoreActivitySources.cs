using System.Diagnostics;

namespace Bookstore.Observability;

// One ActivitySource per module, like the Meters. Registered with AddSource("Bookstore.*").
public static class BookstoreActivitySources
{
    public static readonly ActivitySource Ordering = new("Bookstore.Ordering");
    public static readonly ActivitySource Notifications = new("Bookstore.Notifications");
}
