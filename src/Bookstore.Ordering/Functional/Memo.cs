using System.Collections.Concurrent;

namespace Bookstore.Ordering.Functional;

public static class Memo
{
    public static Func<TIn, TOut> Memoize<TIn, TOut>(this Func<TIn, TOut> function) where TIn : notnull
    {
        ConcurrentDictionary<TIn, TOut> cache = new();
        return input => cache.GetOrAdd(input, function);
    }
}
