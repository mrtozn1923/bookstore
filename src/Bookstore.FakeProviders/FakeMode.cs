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
    Drop,
    // Webhook receiver only: 410, "stop sending me events".
    Gone
}
