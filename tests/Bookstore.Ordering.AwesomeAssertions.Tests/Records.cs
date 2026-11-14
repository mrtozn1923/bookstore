namespace Bookstore.Ordering.AwesomeAssertions.Tests;

internal static class Records
{
    public const string Dune = "9780441172719";
    public const string Hobbit = "9780261103344";
    public const string Emma = "9780141439587";

    public static OrderRecord SavedOrder() => new()
    {
        Id = Guid.Parse("8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11"),
        OrderNumber = "BS-2026-000123",
        CustomerEmail = "ada@example.com",
        Status = OrderStatus.Placed,
        CreatedAtUtc = new DateTimeOffset(2026, 11, 14, 9, 30, 0, TimeSpan.Zero),
        CreatedBy = "checkout-api",
        Lines =
        [
            new() { Id = Guid.Parse("0f1e2d3c-0000-4000-8000-000000000001"), Isbn = Dune, Title = "Dune", Quantity = 2, UnitPrice = 12.4950m },
            new() { Id = Guid.Parse("0f1e2d3c-0000-4000-8000-000000000002"), Isbn = Hobbit, Title = "The Hobbit", Quantity = 1, UnitPrice = 9.9000m },
            new() { Id = Guid.Parse("0f1e2d3c-0000-4000-8000-000000000003"), Isbn = Emma, Title = "Emma", Quantity = 1, UnitPrice = 8.7499m },
        ],
    };
}
