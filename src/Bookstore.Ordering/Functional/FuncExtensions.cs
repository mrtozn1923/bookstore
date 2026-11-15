namespace Bookstore.Ordering.Functional;

public static class FuncExtensions
{
    // f.Then(g): first f, then g
    public static Func<TIn, TOut> Then<TIn, TMid, TOut>(this Func<TIn, TMid> first, Func<TMid, TOut> second) =>
        input => second(first(input));

    // g.Compose(f): g after f, the mathematical order
    public static Func<TIn, TOut> Compose<TIn, TMid, TOut>(this Func<TMid, TOut> second, Func<TIn, TMid> first) =>
        input => second(first(input));

    public static TOut Pipe<TIn, TOut>(this TIn value, Func<TIn, TOut> function) => function(value);

    public static Func<T1, Func<T2, TOut>> Curry<T1, T2, TOut>(this Func<T1, T2, TOut> function) =>
        first => second => function(first, second);

    public static Func<T2, TOut> Partial<T1, T2, TOut>(this Func<T1, T2, TOut> function, T1 first) =>
        second => function(first, second);
}
