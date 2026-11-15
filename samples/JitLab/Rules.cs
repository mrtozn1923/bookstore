using System.Runtime.CompilerServices;
using Bookstore.Catalog;

namespace Bookstore.JitLab;

public interface IBookRule
{
    bool Matches(Book book);
}

public sealed class InStock : IBookRule
{
    public bool Matches(Book book) => book.Stock > 0;
}

public sealed class PublishedBefore(int year) : IBookRule
{
    public bool Matches(Book book) => book.Year < year;
}

public static class Lab
{
    // interface call: the JIT does not know which class is behind 'rule'
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CountMatches(Book[] books, IBookRule rule)
    {
        int count = 0;
        foreach (Book book in books)
        {
            if (rule.Matches(book))
                count++;
        }
        return count;
    }

    // i < books.Length: the JIT can prove every index is valid
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CountInStock(Book[] books)
    {
        int count = 0;
        for (int i = 0; i < books.Length; i++)
        {
            if (books[i].Stock > 0)
                count++;
        }
        return count;
    }

    // i < take: nothing tells the JIT that take <= books.Length
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CountInStock(Book[] books, int take)
    {
        int count = 0;
        for (int i = 0; i < take; i++)
        {
            if (books[i].Stock > 0)
                count++;
        }
        return count;
    }

    // a small array that never leaves the method
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ShippingDays(int quantity)
    {
        int[] limits = [1, 5, 20];
        int days = 1;
        foreach (int limit in limits)
        {
            if (quantity > limit)
                days++;
        }
        return days;
    }

    // a lambda that captures 'limit': closure object + delegate
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CountCheaperThan(Book[] books, decimal limit)
    {
        Func<Book, bool> cheap = book => book.Price < limit;
        int count = 0;
        foreach (Book book in books)
        {
            if (cheap(book))
                count++;
        }
        return count;
    }

    // an array seen through IEnumerable<Book>: foreach asks for an enumerator object
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CountInStockSeq(Book[] books)
    {
        IEnumerable<Book> sequence = books;
        int count = 0;
        foreach (Book book in sequence)
        {
            if (book.Stock > 0)
                count++;
        }
        return count;
    }
}
