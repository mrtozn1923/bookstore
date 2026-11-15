using System.Runtime.CompilerServices;
using Bookstore.Catalog;

namespace Bookstore.GcLab;

public static class LabCatalog
{
    // a bigger catalog built from the sample books: prices and stock vary, the authors repeat
    public static Book[] Generate(int count) =>
        [.. Enumerable.Range(0, count).Select(i =>
        {
            Book sample = SampleBooks.All[i % SampleBooks.All.Count];
            return sample with { Price = sample.Price + i % 100, Stock = i % 7 };
        })];
}

public sealed class Shelf
{
    public Book? Latest;
    public int Count;

    // a reference store goes through the write barrier, an int store does not
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Put(Book book)
    {
        Latest = book;
        Count++;
    }
}
