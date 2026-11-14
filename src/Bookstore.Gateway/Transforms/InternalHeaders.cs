namespace Bookstore.Gateway.Transforms;

public static class InternalHeaders
{
    // Every header with this prefix is set by the gateway only; clients cannot send them.
    public const string Prefix = "X-Bookstore-";
    public const string Route = "X-Bookstore-Route";
}
