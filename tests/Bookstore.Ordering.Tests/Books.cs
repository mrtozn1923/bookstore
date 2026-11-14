namespace Bookstore.Ordering.Tests;

internal static class Books
{
    public const string Dune = "9780441172719";
    public const string Hobbit = "9780261103344";
    public const string Emma = "9780141439587";

    public static Order DuneAndHobbit() => new(
        Guid.Parse("8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11"),
        "BS-2026-000123",
        " Ada@Example.com",
        [new OrderLine(Dune, 2, 12.50m), new OrderLine(Hobbit, 1, 9.90m)]);
}
