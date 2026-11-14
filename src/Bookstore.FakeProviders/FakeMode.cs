namespace Bookstore.FakeProviders;

public enum FakeMode
{
    Normal,
    Slow,
    Error,
    Throttle,
    Maintenance,
    SchemaChanged,
    // Payments only: the request never reaches the provider's logic.
    Drop
}
