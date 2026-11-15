using System.Runtime.CompilerServices;

Book[] books =
[
    new("Dune", 349.90m),
    new("Emma", 199.00m),
    new("The Hobbit", 289.50m),
];

int matches = 0;
for (int round = 1; round <= 5; round++)
{
    for (int i = 0; i < 100; i++)
    {
        foreach (Book book in books)
        {
            if (Filters.IsCheap(book))
                matches++;
        }
    }

    // give the background tiering thread time to recompile hot methods
    Thread.Sleep(200);
}

Console.WriteLine($"IsCheap calls: {5 * 100 * books.Length}, matches: {matches}");

public sealed record Book(string Title, decimal Price);

public static class Filters
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsCheap(Book book) => book.Price < 300m;
}
